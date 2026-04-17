using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Минимальный MAVLink клиент (v1 + v2) для чтения/установки параметров дрона и отправки команд.
    /// На приём принимает оба протокола (с корректной обработкой v2 trailing-zero truncation
    /// и пропуском signed-пакетов). На отправку — v1 по умолчанию; v2 можно включить флагом
    /// <see cref="UseV2ForSending"/> (нужно для прошивок, отвечающих только в v2).
    /// Все асинхронные методы поддерживают CancellationToken и корректно возвращают false при таймауте/сбое.
    /// </summary>
    public class MavLinkService
    {
        private const byte MAVLINK_V1_STX = 0xFE;
        private const byte MAVLINK_V2_STX = 0xFD;
        /// <summary>Флаг MAVLINK_IFLAG_SIGNED — signed-пакеты (нужна криптоподпись) мы не поддерживаем.</summary>
        private const byte MAVLINK_IFLAG_SIGNED = 0x01;
        /// <summary>Подушка безопасности для zero-padding payload при приёме усечённого v2-пакета.
        /// Больше любого реально используемого сообщения (самое длинное — COMMAND_LONG = 33 байта).</summary>
        private const int MAX_PAYLOAD_SAFE = 280;

        /// <summary>
        /// Отправлять пакеты в формате MAVLink v2 (STX=0xFD). По умолчанию false (v1, совместимо со всеми прошивками).
        /// Включите, если дрон настроен на "v2-only" (ArduPilot SERIAL*_PROTOCOL с force-v2 или PX4 MAV_PROTO_VER=2).
        /// </summary>
        public static bool UseV2ForSending { get; set; } = false;
        private const byte MSG_ID_PARAM_REQUEST_READ = 20;
        private const byte MSG_ID_PARAM_VALUE = 22;
        private const byte MSG_ID_PARAM_SET = 23;
        private const byte MSG_ID_HEARTBEAT = 0;
        private const byte CRC_EXTRA_PARAM_REQUEST_READ = 214;
        private const byte CRC_EXTRA_PARAM_VALUE = 220;
        private const byte CRC_EXTRA_PARAM_SET = 168;
        private const byte CRC_EXTRA_HEARTBEAT = 50;
        private const byte CRC_EXTRA_COMMAND_LONG = 152;
        private const byte MSG_ID_COMMAND_LONG = 76;
        private const byte MSG_ID_COMMAND_ACK = 77;
        private const byte CRC_EXTRA_COMMAND_ACK = 143;
        private const ushort MAV_CMD_DO_SET_MODE = 176;
        private const ushort MAV_CMD_COMPONENT_ARM_DISARM = 400;
        private const float MAV_MODE_FLAG_CUSTOM_MODE_ENABLED = 1f;
        private const byte MAV_PARAM_TYPE_INT8 = 1;
        private const byte MAV_PARAM_TYPE_REAL32 = 9;
        private const byte MSG_ID_RC_CHANNELS_OVERRIDE = 70;
        private const byte CRC_EXTRA_RC_CHANNELS_OVERRIDE = 124;
        private const ushort CHAN_NOCHANGE = 65535;
        private const byte MSG_ID_STATUSTEXT = 253;
        private const byte CRC_EXTRA_STATUSTEXT = 83;

        private const byte GCS_SYS_ID = 255;
        private const byte GCS_COMP_ID = 190;

        /// <summary>
        /// Разбирает MAVLink v1 или v2 пакет и возвращает payload, дополненный нулями до <see cref="MAX_PAYLOAD_SAFE"/>.
        /// Это важно для v2, где применяется trailing-zero truncation — без padding'а
        /// фиксированные смещения (например, param_id в байтах 8..23 для PARAM_VALUE) могут уйти за границу буфера.
        /// Signed-пакеты (incompat_flags &amp; 0x01) отбрасываются.
        /// </summary>
        /// <param name="data">Сырой буфер пакета.</param>
        /// <param name="length">Фактическая длина данных в буфере.</param>
        /// <param name="msgId">Выход: MAVLink message id.</param>
        /// <param name="payload">Выход: зеро-падженный payload (первые N байт — реальные, остальные — нули).</param>
        /// <returns>true, если формат валиден и пакет поддерживается.</returns>
        private static bool TryParseMavLink(byte[] data, int length, out byte msgId, out byte[] payload)
        {
            msgId = 0;
            payload = null;
            if (data == null || length < 8) return false;

            int payloadOffset;
            int payloadLen;

            if (data[0] == MAVLINK_V1_STX)
            {
                payloadLen = data[1];
                msgId = data[5];
                payloadOffset = 6;
                if (length < 6 + payloadLen + 2) return false;
            }
            else if (data[0] == MAVLINK_V2_STX && length >= 12)
            {
                byte incompatFlags = data[2];
                // Подписанные v2-пакеты (флаг 0x01) мы не умеем валидировать — пропускаем,
                // чтобы не принять подменённый пакет за валидный и не ломиться в signature-хвост.
                if ((incompatFlags & MAVLINK_IFLAG_SIGNED) != 0) return false;
                payloadLen = data[1];
                uint msgId32 = (uint)data[7] | ((uint)data[8] << 8) | ((uint)data[9] << 16);
                if (msgId32 > 255) return false;
                msgId = (byte)msgId32;
                payloadOffset = 10;
                if (length < 10 + payloadLen + 2) return false;
            }
            else
            {
                return false;
            }

            // Зеро-падим payload до безопасной длины, чтобы чтение по фиксированным смещениям
            // всегда было в пределах буфера (v2 может прислать усечённый payload).
            payload = new byte[MAX_PAYLOAD_SAFE];
            if (payloadLen > 0)
                Array.Copy(data, payloadOffset, payload, 0, Math.Min(payloadLen, MAX_PAYLOAD_SAFE));
            return true;
        }

        private static ushort Crc16MavLink(byte[] data, int offset, int length, byte crcExtra)
        {
            ushort crc = 0xFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                byte tmp = (byte)(data[i] ^ (crc & 0xFF));
                tmp ^= (byte)(tmp << 4);
                crc = (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
            }
            byte tmp2 = (byte)(crcExtra ^ (crc & 0xFF));
            tmp2 ^= (byte)(tmp2 << 4);
            crc = (ushort)((crc >> 8) ^ (tmp2 << 8) ^ (tmp2 << 3) ^ (tmp2 >> 4));
            return crc;
        }

        /// <summary>
        /// Неблокирующий приём одного UDP-пакета. Возвращает null, если ничего не пришло за poll-интервал.
        /// Обрабатывает обычные состояния (таймаут/connection reset) без исключений наверх.
        /// </summary>
        private static async Task<byte[]> TryReceiveAsync(UdpClient client, int pollMs, CancellationToken ct)
        {
            try
            {
                if (client.Available > 0)
                {
                    var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    return client.Receive(ref remoteEp);
                }
            }
            catch (SocketException)
            {
                // ICMP port unreachable / socket reset — игнорируем, повторим
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            try { await Task.Delay(Math.Max(1, pollMs), ct); }
            catch (OperationCanceledException) { }
            return null;
        }

        private static async Task SendHeartbeatsAsync(UdpClient client, ref byte seq, int count, int intervalMs, CancellationToken ct)
        {
            for (int i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    byte[] heartbeat = BuildHeartbeatPacket(seq++, GCS_SYS_ID, GCS_COMP_ID);
                    client.Send(heartbeat, heartbeat.Length);
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { return; }
                try { await Task.Delay(Math.Max(1, intervalMs), ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>
        /// Читает параметр дрона по MAVLink (UDP). UDP может терять пакеты, поэтому реализован повторный запрос.
        /// </summary>
        public static async Task<float?> ReadParameterAsync(
            string host,
            int port,
            string paramName,
            int timeoutMs = 15000,
            Action<string> log = null,
            CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                log("[MAVLink] Ошибка: не задан host или недопустимый порт.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(paramName))
            {
                log("[MAVLink] Ошибка: не задано имя параметра.");
                return null;
            }

            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);

                    byte seq = 0;
                    await SendHeartbeatsAsync(client, ref seq, 2, DelaySettings.MavLink_HeartbeatInterval, ct);
                    if (ct.IsCancellationRequested) return null;
                    try { await Task.Delay(DelaySettings.MavLink_AfterHeartbeat, ct); }
                    catch (OperationCanceledException) { return null; }

                    byte[] paramIdBytes = BuildParamIdBytes(paramName);
                    byte[] payload = new byte[20];
                    BitConverter.GetBytes((short)-1).CopyTo(payload, 0);
                    payload[2] = 1;
                    payload[3] = 1;
                    Array.Copy(paramIdBytes, 0, payload, 4, 16);

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1000, timeoutMs));
                    // Повторная отправка запроса примерно в середине таймаута на случай потери пакета.
                    DateTime nextResend = DateTime.UtcNow.AddMilliseconds(Math.Max(500, timeoutMs / 2));

                    byte[] packet = BuildMavLinkPacket(MSG_ID_PARAM_REQUEST_READ, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_PARAM_REQUEST_READ);
                    try { client.Send(packet, packet.Length); }
                    catch (SocketException ex) { log($"[MAVLink] Ошибка отправки запроса: {ex.Message}"); return null; }

                    log($"[MAVLink] Запрос параметра '{paramName}'...");

                    while (DateTime.UtcNow < deadline)
                    {
                        if (ct.IsCancellationRequested) return null;

                        if (DateTime.UtcNow >= nextResend)
                        {
                            try
                            {
                                byte[] retry = BuildMavLinkPacket(MSG_ID_PARAM_REQUEST_READ, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_PARAM_REQUEST_READ);
                                client.Send(retry, retry.Length);
                                log($"[MAVLink] Повторный запрос '{paramName}' (UDP может терять пакеты)...");
                            }
                            catch (SocketException) { }
                            nextResend = DateTime.UtcNow.AddMilliseconds(Math.Max(500, timeoutMs / 2));
                        }

                        var received = await TryReceiveAsync(client, DelaySettings.MavLink_ReadPollInterval, ct);
                        if (received == null) continue;

                        if (!TryParseMavLink(received, received.Length, out byte msgId, out byte[] payloadBuf)) continue;
                        if (msgId != MSG_ID_PARAM_VALUE) continue;

                        // В v2 payload может быть усечён (trailing zeros); payloadBuf уже zero-padded в TryParseMavLink.
                        string recvParamId = Encoding.ASCII.GetString(payloadBuf, 8, 16).TrimEnd('\0');
                        if (!string.Equals(recvParamId, paramName, StringComparison.OrdinalIgnoreCase)) continue;

                        float value = BitConverter.ToSingle(payloadBuf, 0);
                        log($"[MAVLink] {paramName} = {value}");
                        if (DelaySettings.MavLink_ParamRetrievalDelayMs > 0)
                        {
                            try { await Task.Delay(DelaySettings.MavLink_ParamRetrievalDelayMs, ct); }
                            catch (OperationCanceledException) { }
                        }
                        return value;
                    }

                    log($"[MAVLink] Таймаут: параметр '{paramName}' не получен за {timeoutMs} мс.");
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                log("[MAVLink] Операция отменена.");
                return null;
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка чтения параметра: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static byte[] BuildParamIdBytes(string paramName)
        {
            byte[] result = new byte[16];
            byte[] nameBytes = Encoding.ASCII.GetBytes(paramName ?? "");
            Array.Copy(nameBytes, 0, result, 0, Math.Min(16, nameBytes.Length));
            return result;
        }

        private static byte[] BuildHeartbeatPacket(byte seq, byte sysId, byte compId)
        {
            byte[] payload = new byte[9];
            payload[4] = 6; // MAV_TYPE_GCS в wire-порядке (после reorder): байт 4 = type
            return BuildMavLinkPacket(MSG_ID_HEARTBEAT, payload, seq, sysId, compId, CRC_EXTRA_HEARTBEAT);
        }

        /// <summary>
        /// Собирает MAVLink-пакет. Формат зависит от <see cref="UseV2ForSending"/>:
        /// <list type="bullet">
        /// <item>v1 (default): STX=0xFE, header 6 байт, msgId 1 байт.</item>
        /// <item>v2: STX=0xFD, header 10 байт, incompat/compat=0, msgId 3 байта, без signature.</item>
        /// </list>
        /// CRC-алгоритм (CRC-16/MCRF4XX + crc_extra) одинаков для обеих версий.
        /// На отправке payload НЕ усекаем по хвостовым нулям — receiver обработает полную длину и в v2.
        /// </summary>
        private static byte[] BuildMavLinkPacket(byte msgId, byte[] payload, byte seq, byte sysId, byte compId, byte crcExtra)
        {
            return UseV2ForSending
                ? BuildMavLinkPacketV2(msgId, payload, seq, sysId, compId, crcExtra)
                : BuildMavLinkPacketV1(msgId, payload, seq, sysId, compId, crcExtra);
        }

        private static byte[] BuildMavLinkPacketV1(byte msgId, byte[] payload, byte seq, byte sysId, byte compId, byte crcExtra)
        {
            int payloadLen = payload?.Length ?? 0;
            byte[] header = new byte[] { MAVLINK_V1_STX, (byte)payloadLen, seq, sysId, compId, msgId };
            int crcLen = header.Length - 1 + payloadLen;
            byte[] crcData = new byte[crcLen];
            Array.Copy(header, 1, crcData, 0, 5);
            if (payload != null && payloadLen > 0)
                Array.Copy(payload, 0, crcData, 5, payloadLen);

            ushort crc = Crc16MavLink(crcData, 0, crcLen, crcExtra);
            byte[] crcBytes = BitConverter.GetBytes(crc);

            byte[] packet = new byte[6 + payloadLen + 2];
            Array.Copy(header, 0, packet, 0, 6);
            if (payload != null && payloadLen > 0)
                Array.Copy(payload, 0, packet, 6, payloadLen);
            packet[6 + payloadLen] = crcBytes[0];
            packet[6 + payloadLen + 1] = crcBytes[1];

            return packet;
        }

        private static byte[] BuildMavLinkPacketV2(byte msgId, byte[] payload, byte seq, byte sysId, byte compId, byte crcExtra)
        {
            int payloadLen = payload?.Length ?? 0;
            if (payloadLen > 255) payloadLen = 255; // hard-лимит протокола
            // header: STX | len | incompat_flags | compat_flags | seq | sysId | compId | msgId(3 байта LE)
            byte[] header = new byte[10]
            {
                MAVLINK_V2_STX,
                (byte)payloadLen,
                0, // incompat_flags — без подписи
                0, // compat_flags
                seq,
                sysId,
                compId,
                msgId,
                0, // msgId high-byte 1
                0  // msgId high-byte 2
            };
            int crcLen = header.Length - 1 + payloadLen; // всё кроме STX
            byte[] crcData = new byte[crcLen];
            Array.Copy(header, 1, crcData, 0, 9);
            if (payload != null && payloadLen > 0)
                Array.Copy(payload, 0, crcData, 9, payloadLen);

            ushort crc = Crc16MavLink(crcData, 0, crcLen, crcExtra);
            byte[] crcBytes = BitConverter.GetBytes(crc);

            byte[] packet = new byte[10 + payloadLen + 2];
            Array.Copy(header, 0, packet, 0, 10);
            if (payload != null && payloadLen > 0)
                Array.Copy(payload, 0, packet, 10, payloadLen);
            packet[10 + payloadLen] = crcBytes[0];
            packet[10 + payloadLen + 1] = crcBytes[1];

            return packet;
        }

        /// <summary>
        /// Устанавливает параметр дрона по MAVLink и ждёт PARAM_VALUE-подтверждения.
        /// При отсутствии ACK повторяет команду до 3 раз.
        /// Возвращает true ТОЛЬКО если получен ACK (подтверждение значения).
        /// </summary>
        public static async Task<bool> SetParameterAsync(
            string host,
            int port,
            string paramName,
            float value,
            byte targetSystem = 1,
            byte targetComponent = 1,
            int timeoutMs = 5000,
            Action<string> log = null,
            byte paramType = MAV_PARAM_TYPE_REAL32,
            CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535 || string.IsNullOrWhiteSpace(paramName))
            {
                log("[MAVLink] Ошибка: некорректные параметры SetParameter.");
                return false;
            }

            const int maxAttempts = 3;
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);

                    byte seq = 0;
                    await SendHeartbeatsAsync(client, ref seq, 2, DelaySettings.MavLink_HeartbeatInterval, ct);
                    if (ct.IsCancellationRequested) return false;
                    try { await Task.Delay(DelaySettings.MavLink_SetAfterHeartbeat, ct); }
                    catch (OperationCanceledException) { return false; }

                    byte[] paramIdBytes = BuildParamIdBytes(paramName);
                    byte[] payload = new byte[23];
                    BitConverter.GetBytes(value).CopyTo(payload, 0);
                    payload[4] = targetSystem;
                    payload[5] = targetComponent;
                    Array.Copy(paramIdBytes, 0, payload, 6, 16);
                    payload[22] = paramType;

                    int perAttemptTimeout = Math.Max(500, timeoutMs);

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        if (ct.IsCancellationRequested) return false;

                        try
                        {
                            byte[] packet = BuildMavLinkPacket(MSG_ID_PARAM_SET, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_PARAM_SET);
                            client.Send(packet, packet.Length);
                        }
                        catch (SocketException ex)
                        {
                            log($"[MAVLink] Ошибка отправки SET: {ex.Message}");
                            return false;
                        }
                        log($"[MAVLink] Установка {paramName} = {value} (попытка {attempt}/{maxAttempts})");

                        DateTime deadline = DateTime.UtcNow.AddMilliseconds(perAttemptTimeout);
                        while (DateTime.UtcNow < deadline)
                        {
                            if (ct.IsCancellationRequested) return false;

                            var received = await TryReceiveAsync(client, DelaySettings.MavLink_ReadPollInterval, ct);
                            if (received == null) continue;
                            if (!TryParseMavLink(received, received.Length, out byte ackMsgId, out byte[] ackPayload)) continue;
                            if (ackMsgId != MSG_ID_PARAM_VALUE) continue;

                            string recvParamId = Encoding.ASCII.GetString(ackPayload, 8, 16).TrimEnd('\0');
                            if (!string.Equals(recvParamId, paramName, StringComparison.OrdinalIgnoreCase)) continue;

                            float ackValue = BitConverter.ToSingle(ackPayload, 0);
                            log($"[MAVLink] ACK: {paramName} = {ackValue}");
                            if (DelaySettings.MavLink_SetAfterSend > 0)
                            {
                                try { await Task.Delay(DelaySettings.MavLink_SetAfterSend, ct); }
                                catch (OperationCanceledException) { }
                            }
                            return true;
                        }

                        if (attempt < maxAttempts)
                            log($"[MAVLink] ACK не получен для {paramName}, повтор...");
                    }

                    log($"[MAVLink] Установка {paramName} = {value} НЕ подтверждена дроном после {maxAttempts} попыток.");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                log("[MAVLink] Операция установки отменена.");
                return false;
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка установки: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Читает параметр, переключает true↔false и записывает обратно.
        /// Возвращает новое состояние или null при ошибке/таймауте.
        /// </summary>
        public static async Task<bool?> ToggleParameterAsync(
            string host,
            int port,
            string paramName,
            int timeoutMs = 20000,
            Action<string> log = null,
            CancellationToken ct = default)
        {
            log = log ?? (s => { });
            var current = await ReadParameterAsync(host, port, paramName, timeoutMs, log, ct);
            if (!current.HasValue) return null;

            float newValue = (current.Value >= 0.5f) ? 0f : 1f;
            string state = newValue >= 0.5f ? "true" : "false";
            log($"[MAVLink] Текущее: {current.Value}, переключаю на {state}");

            bool ok = await SetParameterAsync(host, port, paramName, newValue, 1, 1, timeoutMs, log, MAV_PARAM_TYPE_INT8, ct);
            return ok ? (bool?)(newValue >= 0.5f) : null;
        }

        /// <summary>
        /// Переключает режим полёта через MAV_CMD_DO_SET_MODE.
        /// </summary>
        public static async Task<bool> SetModeAsync(
            string host,
            int port,
            int modeNumber,
            byte targetSystem = 0,
            byte targetComponent = 0,
            Action<string> log = null,
            CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                log("[MAVLink] Ошибка: некорректные параметры SetMode.");
                return false;
            }
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);

                    byte seq = 0;
                    await SendHeartbeatsAsync(client, ref seq, 2, DelaySettings.MavLink_HeartbeatInterval, ct);
                    if (ct.IsCancellationRequested) return false;
                    try { await Task.Delay(DelaySettings.MavLink_CommandAfterHeartbeat, ct); }
                    catch (OperationCanceledException) { return false; }

                    byte[] payload = new byte[33];
                    int offset = 0;
                    BitConverter.GetBytes(MAV_MODE_FLAG_CUSTOM_MODE_ENABLED).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes((float)modeNumber).CopyTo(payload, offset); offset += 4;
                    for (int i = 0; i < 5; i++) { BitConverter.GetBytes(0f).CopyTo(payload, offset); offset += 4; }
                    BitConverter.GetBytes((ushort)MAV_CMD_DO_SET_MODE).CopyTo(payload, offset); offset += 2;
                    payload[offset++] = targetSystem;
                    payload[offset++] = targetComponent;
                    payload[offset] = 0;

                    try
                    {
                        byte[] packet = BuildMavLinkPacket(MSG_ID_COMMAND_LONG, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_COMMAND_LONG);
                        client.Send(packet, packet.Length);
                    }
                    catch (SocketException ex)
                    {
                        log($"[MAVLink] Ошибка отправки SET_MODE: {ex.Message}");
                        return false;
                    }

                    log($"[MAVLink] Режим полёта -> {modeNumber}");
                    try { await Task.Delay(DelaySettings.MavLink_SetAfterSend, ct); }
                    catch (OperationCanceledException) { return false; }
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка смены режима: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Включает моторы (arm).
        /// </summary>
        public static async Task<bool> ArmAsync(string host, int port, bool force = false, Action<string> log = null, CancellationToken ct = default)
        {
            log = log ?? (s => { });
            bool ok = await SendCommandLongAsync(host, port, MAV_CMD_COMPONENT_ARM_DISARM, 1f, force ? 21196f : 0f, 0, 0, 0, 0, 0, log, ct);
            if (!ok) return false;
            try { await Task.Delay(DelaySettings.MavLink_ArmDisarmAfter, ct); }
            catch (OperationCanceledException) { return false; }
            return true;
        }

        /// <summary>
        /// Выключает моторы (disarm).
        /// </summary>
        public static async Task<bool> DisarmAsync(string host, int port, bool force = false, Action<string> log = null, CancellationToken ct = default)
        {
            log = log ?? (s => { });
            bool ok = await SendCommandLongAsync(host, port, MAV_CMD_COMPONENT_ARM_DISARM, 0f, force ? 21196f : 0f, 0, 0, 0, 0, 0, log, ct);
            if (!ok) return false;
            try { await Task.Delay(DelaySettings.MavLink_ArmDisarmAfter, ct); }
            catch (OperationCanceledException) { return false; }
            return true;
        }

        /// <summary>
        /// Отправка RC override. Канал 1-8, PWM 1000-2000. Остальные каналы — без изменений (65535).
        /// </summary>
        public static async Task<bool> SendRcOverrideAsync(string host, int port, int channel, ushort pwm,
            byte targetSystem = 1, byte targetComponent = 1, Action<string> log = null, CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                log("[MAVLink] Ошибка: некорректные параметры RC.");
                return false;
            }
            if (channel < 1 || channel > 8)
            {
                log($"[MAVLink] Ошибка: канал RC должен быть 1..8 (получено {channel}).");
                return false;
            }
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);
                    byte seq = 0;
                    await SendHeartbeatsAsync(client, ref seq, 2, DelaySettings.MavLink_RcHeartbeat, ct);
                    if (ct.IsCancellationRequested) return false;

                    ushort[] channels = new ushort[8];
                    for (int i = 0; i < 8; i++) channels[i] = CHAN_NOCHANGE;
                    channels[channel - 1] = pwm;

                    byte[] payload = new byte[18];
                    int off = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        BitConverter.GetBytes(channels[i]).CopyTo(payload, off);
                        off += 2;
                    }
                    payload[16] = targetSystem;
                    payload[17] = targetComponent;

                    try
                    {
                        byte[] packet = BuildMavLinkPacket(MSG_ID_RC_CHANNELS_OVERRIDE, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_RC_CHANNELS_OVERRIDE);
                        client.Send(packet, packet.Length);
                    }
                    catch (SocketException ex)
                    {
                        log($"[MAVLink] Ошибка отправки RC: {ex.Message}");
                        return false;
                    }

                    log($"[MAVLink] RC ch{channel} = {pwm}");
                    try { await Task.Delay(DelaySettings.MavLink_RcAfterSend, ct); }
                    catch (OperationCanceledException) { return false; }
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка RC: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> SendCommandLongAsync(string host, int port, ushort command,
            float p1, float p2, float p3, float p4, float p5, float p6, float p7, Action<string> log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                log?.Invoke("[MAVLink] Ошибка: некорректные параметры команды.");
                return false;
            }
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);
                    byte seq = 0;
                    await SendHeartbeatsAsync(client, ref seq, 2, DelaySettings.MavLink_HeartbeatInterval, ct);
                    if (ct.IsCancellationRequested) return false;
                    try { await Task.Delay(DelaySettings.MavLink_CommandAfterHeartbeat, ct); }
                    catch (OperationCanceledException) { return false; }

                    byte[] payload = new byte[33];
                    int offset = 0;
                    BitConverter.GetBytes(p1).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p2).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p3).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p4).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p5).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p6).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(p7).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes(command).CopyTo(payload, offset); offset += 2;
                    payload[offset++] = 1;   // target_system
                    payload[offset++] = 1;   // target_component
                    payload[offset] = 0;    // confirmation

                    try
                    {
                        byte[] packet = BuildMavLinkPacket(MSG_ID_COMMAND_LONG, payload, seq++, GCS_SYS_ID, GCS_COMP_ID, CRC_EXTRA_COMMAND_LONG);
                        client.Send(packet, packet.Length);
                    }
                    catch (SocketException ex)
                    {
                        log?.Invoke($"[MAVLink] Ошибка отправки команды: {ex.Message}");
                        return false;
                    }
                    try { await Task.Delay(DelaySettings.MavLink_CommandAfterSend, ct); }
                    catch (OperationCanceledException) { return false; }
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[MAVLink] Ошибка команды: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ожидает STATUSTEXT с подстрокой. Возвращает true, если найдено до таймаута.
        /// </summary>
        public static async Task<bool> WaitForAsync(string host, int port, string substring, int timeoutMs = 10000,
            Action<string> log = null, CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrEmpty(substring)) return true;
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
            {
                log("[MAVLink] Ошибка: некорректные параметры WaitFor.");
                return false;
            }
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);
                    byte seq = 0;
                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(500, timeoutMs));

                    while (DateTime.UtcNow < deadline)
                    {
                        if (ct.IsCancellationRequested) return false;

                        if (client.Available == 0)
                        {
                            try
                            {
                                byte[] hb = BuildHeartbeatPacket(seq++, GCS_SYS_ID, GCS_COMP_ID);
                                client.Send(hb, hb.Length);
                            }
                            catch (SocketException) { }
                            try { await Task.Delay(DelaySettings.MavLink_WaitForHeartbeat, ct); }
                            catch (OperationCanceledException) { return false; }
                            continue;
                        }

                        var received = await TryReceiveAsync(client, DelaySettings.MavLink_WaitForPoll, ct);
                        if (received == null) continue;
                        if (!TryParseMavLink(received, received.Length, out byte wfMsgId, out byte[] wfPayload)) continue;
                        if (wfMsgId != MSG_ID_STATUSTEXT) continue;

                        // STATUSTEXT: severity (1 байт) + text (50 байт ASCII, zero-terminated).
                        // В v2 поле может быть усечено — но wfPayload у нас уже zero-padded, так что чтение всегда безопасно.
                        string text = Encoding.ASCII.GetString(wfPayload, 1, 50).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(text) && text.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            log($"[MAVLink] WaitFor: найдено '{substring}' в '{text}'");
                            return true;
                        }
                    }
                    log($"[MAVLink] WaitFor: таймаут, подстрока '{substring}' не найдена");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                log($"[MAVLink] WaitFor ошибка: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, отвечает ли дрон по MAVLink (heartbeat или любой другой валидный пакет).
        /// </summary>
        public static async Task<bool> CheckConnectionAsync(string host, int port, int timeoutMs = 5000, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535)
                return false;
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(500, timeoutMs));
                    DateTime nextHeartbeat = DateTime.MinValue;
                    byte seq = 0;

                    while (DateTime.UtcNow < deadline)
                    {
                        if (ct.IsCancellationRequested) return false;

                        if (DateTime.UtcNow >= nextHeartbeat)
                        {
                            try
                            {
                                byte[] heartbeat = BuildHeartbeatPacket(seq++, GCS_SYS_ID, GCS_COMP_ID);
                                client.Send(heartbeat, heartbeat.Length);
                            }
                            catch (SocketException) { }
                            nextHeartbeat = DateTime.UtcNow.AddMilliseconds(1000);
                        }

                        var received = await TryReceiveAsync(client, DelaySettings.MavLink_CheckConnectionPoll, ct);
                        if (received == null) continue;
                        // Принимаем любой валидный MAVLink-пакет (v1/v2) как подтверждение связи.
                        if (TryParseMavLink(received, received.Length, out _, out _))
                            return true;
                    }
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
