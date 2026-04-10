using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Минимальный MAVLink v1 клиент для чтения параметров дрона.
    /// Работает без Python — всё встроено в приложение.
    /// </summary>
    public class MavLinkService
    {
        private const byte MAVLINK_V1_STX = 0xFE;
        private const byte MAVLINK_V2_STX = 0xFD;
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
        private const ushort MAV_CMD_DO_SET_MODE = 176;
        private const ushort MAV_CMD_COMPONENT_ARM_DISARM = 400;
        private const float MAV_MODE_FLAG_CUSTOM_MODE_ENABLED = 1f;
        private const byte MAV_PARAM_TYPE_INT8 = 1;
        private const byte MSG_ID_RC_CHANNELS_OVERRIDE = 70;
        private const byte CRC_EXTRA_RC_CHANNELS_OVERRIDE = 124;
        private const ushort CHAN_NOCHANGE = 65535;
        private const byte MSG_ID_STATUSTEXT = 253;
        private const byte CRC_EXTRA_STATUSTEXT = 83;

        /// <summary>
        /// Разбирает MAVLink v1 или v2 пакет. Возвращает true если формат валиден.
        /// </summary>
        private static bool TryParseMavLink(byte[] data, out byte msgId, out int payloadOffset, out int payloadLen)
        {
            msgId = 0; payloadOffset = 0; payloadLen = 0;
            if (data == null || data.Length < 8) return false;

            if (data[0] == MAVLINK_V1_STX)
            {
                payloadLen = data[1];
                msgId = data[5];
                payloadOffset = 6;
                return data.Length >= 6 + payloadLen + 2;
            }
            if (data[0] == MAVLINK_V2_STX && data.Length >= 12)
            {
                payloadLen = data[1];
                uint msgId32 = (uint)data[7] | ((uint)data[8] << 8) | ((uint)data[9] << 16);
                if (msgId32 > 255) return false;
                msgId = (byte)msgId32;
                payloadOffset = 10;
                return data.Length >= 10 + payloadLen + 2;
            }
            return false;
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
        /// Читает параметр дрона по MAVLink (UDP).
        /// </summary>
        /// <param name="host">IP дрона (например 192.168.4.1)</param>
        /// <param name="port">Порт MAVLink (обычно 14550)</param>
        /// <param name="paramName">Имя параметра (например SERVO1_REVERSED)</param>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <param name="log">Опциональный логгер</param>
        /// <returns>Значение параметра или null при ошибке</returns>
        public static async Task<float?> ReadParameterAsync(
            string host,
            int port,
            string paramName,
            int timeoutMs = 15000,
            Action<string> log = null)
        {
            log = log ?? (s => { });
            try
            {
                using (var client = new UdpClient())
                {
                    client.Client.ReceiveTimeout = timeoutMs;
                    client.Connect(host, port);

                    byte seq = 0;

                    var remoteEp = new IPEndPoint(IPAddress.Any, 0);

                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        byte[] heartbeat = BuildHeartbeatPacket(seq++, 255, 190);
                        client.Send(heartbeat, heartbeat.Length);
                        await Task.Delay(DelaySettings.MavLink_HeartbeatInterval);
                    }

                    await Task.Delay(DelaySettings.MavLink_AfterHeartbeat);

                    byte[] paramIdBytes = new byte[16];
                    byte[] nameBytes = Encoding.ASCII.GetBytes(paramName ?? "");
                    int copyLen = Math.Min(16, nameBytes.Length);
                    Array.Copy(nameBytes, 0, paramIdBytes, 0, copyLen);

                    byte[] payload = new byte[20];
                    BitConverter.GetBytes((short)-1).CopyTo(payload, 0);
                    payload[2] = 1;
                    payload[3] = 1;
                    Array.Copy(paramIdBytes, 0, payload, 4, 16);

                    byte[] packet = BuildMavLinkPacket(MSG_ID_PARAM_REQUEST_READ, payload, seq++, 255, 190, CRC_EXTRA_PARAM_REQUEST_READ);
                    client.Send(packet, packet.Length);

                    log($"[MAVLink] Запрос параметра '{paramName}'...");

                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                    while (DateTime.UtcNow < deadline)
                    {
                        if (client.Available > 0)
                        {
                            byte[] received = client.Receive(ref remoteEp);
                            if (TryParseMavLink(received, out byte msgId, out int pOff, out int pLen))
                            {
                                if (msgId == MSG_ID_PARAM_VALUE && pLen >= 25)
                                {
                                    float value = BitConverter.ToSingle(received, pOff);
                                    string recvParamId = Encoding.ASCII.GetString(received, pOff + 8, 16).TrimEnd('\0');
                                    if (string.Equals(recvParamId, paramName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        log($"[MAVLink] {paramName} = {value}");
                                        if (DelaySettings.MavLink_ParamRetrievalDelayMs > 0)
                                            await Task.Delay(DelaySettings.MavLink_ParamRetrievalDelayMs);
                                        return value;
                                    }
                                }
                            }
                        }
                        await Task.Delay(DelaySettings.MavLink_ReadPollInterval);
                    }

                    log("[MAVLink] Таймаут: параметр не получен.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка: {ex.Message}");
                return null;
            }
        }

        private static byte[] BuildHeartbeatPacket(byte seq, byte sysId, byte compId)
        {
            byte[] payload = new byte[9];
            payload[4] = 6;
            return BuildMavLinkPacket(MSG_ID_HEARTBEAT, payload, seq, sysId, compId, CRC_EXTRA_HEARTBEAT);
        }

        private static byte[] BuildMavLinkPacket(byte msgId, byte[] payload, byte seq, byte sysId, byte compId, byte crcExtra)
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

        private const byte MAV_PARAM_TYPE_REAL32 = 9;

        /// <summary>
        /// Устанавливает параметр дрона по MAVLink и ждёт PARAM_VALUE-подтверждения.
        /// При отсутствии ACK повторяет команду до 3 раз.
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
            byte paramType = 9)
        {
            log = log ?? (s => { });
            const int maxAttempts = 3;
            try
            {
                using (var client = new UdpClient())
                {
                    client.Client.ReceiveTimeout = 500;
                    client.Connect(host, port);

                    byte seq = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        client.Send(BuildHeartbeatPacket(seq++, 255, 190), 17);
                        await Task.Delay(DelaySettings.MavLink_HeartbeatInterval);
                    }
                    await Task.Delay(DelaySettings.MavLink_SetAfterHeartbeat);

                    byte[] paramIdBytes = new byte[16];
                    byte[] nameBytes = Encoding.ASCII.GetBytes(paramName ?? "");
                    Array.Copy(nameBytes, 0, paramIdBytes, 0, Math.Min(16, nameBytes.Length));

                    byte[] payload = new byte[23];
                    BitConverter.GetBytes(value).CopyTo(payload, 0);
                    payload[4] = targetSystem;
                    payload[5] = targetComponent;
                    Array.Copy(paramIdBytes, 0, payload, 6, 16);
                    payload[22] = paramType;

                    var remoteEp = new IPEndPoint(IPAddress.Any, 0);

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        byte[] packet = BuildMavLinkPacket(MSG_ID_PARAM_SET, payload, seq++, 255, 190, CRC_EXTRA_PARAM_SET);
                        client.Send(packet, packet.Length);
                        log($"[MAVLink] Установка {paramName} = {value} (попытка {attempt}/{maxAttempts})");

                        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                        while (DateTime.UtcNow < deadline)
                        {
                            if (client.Available > 0)
                            {
                                byte[] received = client.Receive(ref remoteEp);
                                if (TryParseMavLink(received, out byte ackMsgId, out int ackOff, out int ackLen))
                                {
                                    if (ackMsgId == MSG_ID_PARAM_VALUE && ackLen >= 25)
                                    {
                                        string recvParamId = Encoding.ASCII.GetString(received, ackOff + 8, 16).TrimEnd('\0');
                                        if (string.Equals(recvParamId, paramName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            float ackValue = BitConverter.ToSingle(received, ackOff);
                                            log($"[MAVLink] ACK: {paramName} = {ackValue}");
                                            return true;
                                        }
                                    }
                                }
                            }
                            await Task.Delay(DelaySettings.MavLink_ReadPollInterval);
                        }

                        if (attempt < maxAttempts)
                            log($"[MAVLink] ACK не получен для {paramName}, повтор...");
                    }

                    log($"[MAVLink] Установка {paramName} = {value} выполнена (без ACK после {maxAttempts} попыток)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка установки: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Читает параметр, переключает true↔false и записывает обратно.
        /// </summary>
        public static async Task<bool?> ToggleParameterAsync(
            string host,
            int port,
            string paramName,
            int timeoutMs = 20000,
            Action<string> log = null)
        {
            log = log ?? (s => { });
            var current = await ReadParameterAsync(host, port, paramName, timeoutMs, log);
            if (!current.HasValue) return null;

            float newValue = (current.Value >= 0.5f) ? 0f : 1f;
            string state = newValue >= 0.5f ? "true" : "false";
            log($"[MAVLink] Текущее: {current.Value}, переключаю на {state}");

            bool ok = await SetParameterAsync(host, port, paramName, newValue, 1, 1, timeoutMs, log, MAV_PARAM_TYPE_INT8);
            return ok ? (bool?)(newValue >= 0.5f) : null;
        }

        /// <summary>
        /// Переключает режим полёта через MAV_CMD_DO_SET_MODE.
        /// </summary>
        /// <param name="host">IP дрона</param>
        /// <param name="port">Порт MAVLink (14550)</param>
        /// <param name="modeNumber">Номер режима: 0=Stabilize, 1=Acro, 2=AltHold, 3=Auto, 4=Guided, 5=Loiter, 6=RTL, 7=Circle, 9=Land, и т.д.</param>
        /// <param name="targetSystem">0 = текущая система</param>
        /// <param name="targetComponent">0 = все компоненты</param>
        /// <param name="log">Логгер</param>
        public static async Task<bool> SetModeAsync(
            string host,
            int port,
            int modeNumber,
            byte targetSystem = 0,
            byte targetComponent = 0,
            Action<string> log = null)
        {
            log = log ?? (s => { });
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);

                    byte seq = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        client.Send(BuildHeartbeatPacket(seq++, 255, 190), 17);
                        await Task.Delay(DelaySettings.MavLink_HeartbeatInterval);
                    }
                    await Task.Delay(DelaySettings.MavLink_CommandAfterHeartbeat);

                    byte[] payload = new byte[33];
                    int offset = 0;
                    BitConverter.GetBytes(MAV_MODE_FLAG_CUSTOM_MODE_ENABLED).CopyTo(payload, offset); offset += 4;
                    BitConverter.GetBytes((float)modeNumber).CopyTo(payload, offset); offset += 4;
                    for (int i = 0; i < 5; i++) { BitConverter.GetBytes(0f).CopyTo(payload, offset); offset += 4; }
                    BitConverter.GetBytes((ushort)MAV_CMD_DO_SET_MODE).CopyTo(payload, offset); offset += 2;
                    payload[offset++] = targetSystem;
                    payload[offset++] = targetComponent;
                    payload[offset] = 0;

                    byte[] packet = BuildMavLinkPacket(MSG_ID_COMMAND_LONG, payload, seq++, 255, 190, CRC_EXTRA_COMMAND_LONG);
                    client.Send(packet, packet.Length);

                    log($"[MAVLink] Режим полёта -> {modeNumber}");
                    await Task.Delay(DelaySettings.MavLink_SetAfterSend);
                    return true;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка смены режима: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Включает моторы (arm). Аналог Script.ChangeMode / MAV.doARM(true).
        /// </summary>
        public static async Task<bool> ArmAsync(string host, int port, bool force = false, Action<string> log = null)
        {
            return await SendCommandLongAsync(host, port, MAV_CMD_COMPONENT_ARM_DISARM, 1f, force ? 21196f : 0f, 0, 0, 0, 0, 0, log ?? (s => { }))
                && await Task.Delay(DelaySettings.MavLink_ArmDisarmAfter).ContinueWith(_ => true);
        }

        /// <summary>
        /// Выключает моторы (disarm). Аналог MAV.doARM(false).
        /// </summary>
        public static async Task<bool> DisarmAsync(string host, int port, bool force = false, Action<string> log = null)
        {
            return await SendCommandLongAsync(host, port, MAV_CMD_COMPONENT_ARM_DISARM, 0f, force ? 21196f : 0f, 0, 0, 0, 0, 0, log ?? (s => { }))
                && await Task.Delay(DelaySettings.MavLink_ArmDisarmAfter).ContinueWith(_ => true);
        }

        /// <summary>
        /// Отправка RC override. Аналог Script.SendRC(channel, pwm, true).
        /// Канал 1-8, PWM 1000-2000. Остальные каналы = без изменений (65535).
        /// </summary>
        public static async Task<bool> SendRcOverrideAsync(string host, int port, int channel, ushort pwm,
            byte targetSystem = 1, byte targetComponent = 1, Action<string> log = null)
        {
            log = log ?? (s => { });
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);
                    byte seq = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        client.Send(BuildHeartbeatPacket(seq++, 255, 190), 17);
                        await Task.Delay(DelaySettings.MavLink_RcHeartbeat);
                    }

                    ushort c1 = channel == 1 ? pwm : CHAN_NOCHANGE;
                    ushort c2 = channel == 2 ? pwm : CHAN_NOCHANGE;
                    ushort c3 = channel == 3 ? pwm : CHAN_NOCHANGE;
                    ushort c4 = channel == 4 ? pwm : CHAN_NOCHANGE;
                    ushort c5 = channel == 5 ? pwm : CHAN_NOCHANGE;
                    ushort c6 = channel == 6 ? pwm : CHAN_NOCHANGE;
                    ushort c7 = channel == 7 ? pwm : CHAN_NOCHANGE;
                    ushort c8 = channel == 8 ? pwm : CHAN_NOCHANGE;

                    byte[] payload = new byte[18];
                    int off = 0;
                    foreach (var v in new[] { c1, c2, c3, c4, c5, c6, c7, c8 })
                    {
                        BitConverter.GetBytes(v).CopyTo(payload, off);
                        off += 2;
                    }
                    payload[16] = targetSystem;
                    payload[17] = targetComponent;

                    byte[] packet = BuildMavLinkPacket(MSG_ID_RC_CHANNELS_OVERRIDE, payload, seq++, 255, 190, CRC_EXTRA_RC_CHANNELS_OVERRIDE);
                    client.Send(packet, packet.Length);
                    log($"[MAVLink] RC ch{channel} = {pwm}");
                    await Task.Delay(DelaySettings.MavLink_RcAfterSend);
                    return true;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка RC: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> SendCommandLongAsync(string host, int port, ushort command,
            float p1, float p2, float p3, float p4, float p5, float p6, float p7, Action<string> log)
        {
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(host, port);
                    byte seq = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        client.Send(BuildHeartbeatPacket(seq++, 255, 190), 17);
                        await Task.Delay(DelaySettings.MavLink_HeartbeatInterval);
                    }
                    await Task.Delay(DelaySettings.MavLink_CommandAfterHeartbeat);

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

                    byte[] packet = BuildMavLinkPacket(MSG_ID_COMMAND_LONG, payload, seq++, 255, 190, CRC_EXTRA_COMMAND_LONG);
                    client.Send(packet, packet.Length);
                    await Task.Delay(DelaySettings.MavLink_CommandAfterSend);
                    return true;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] Ошибка команды: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ожидает STATUSTEXT с подстрокой. Аналог Script.WaitFor(string, timeout).
        /// </summary>
        public static async Task<bool> WaitForAsync(string host, int port, string substring, int timeoutMs = 10000,
            Action<string> log = null, CancellationToken ct = default)
        {
            log = log ?? (s => { });
            if (string.IsNullOrEmpty(substring)) return true;
            try
            {
                using (var client = new UdpClient())
                {
                    client.Client.ReceiveTimeout = Math.Max(1000, timeoutMs + 2000);
                    client.Connect(host, port);
                    var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    byte seq = 0;
                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

                    while (DateTime.UtcNow < deadline)
                    {
                        if (ct.IsCancellationRequested) return false;
                        if (client.Available == 0)
                        {
                            client.Send(BuildHeartbeatPacket(seq++, 255, 190), 17);
                            await Task.Delay(DelaySettings.MavLink_WaitForHeartbeat, ct);
                            continue;
                        }
                        byte[] received = client.Receive(ref remoteEp);
                        if (!TryParseMavLink(received, out byte wfMsgId, out int wfOff, out int wfLen)) continue;
                        if (wfMsgId != MSG_ID_STATUSTEXT) continue;
                        if (wfLen < 51) continue;
                        string text = Encoding.ASCII.GetString(received, wfOff + 1, Math.Min(50, wfLen - 1)).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(text) && text.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            log($"[MAVLink] WaitFor: найдено '{substring}' в '{text}'");
                            return true;
                        }
                        await Task.Delay(DelaySettings.MavLink_WaitForPoll, ct);
                    }
                    log($"[MAVLink] WaitFor: таймаут, подстрока '{substring}' не найдена");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log($"[MAVLink] WaitFor ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, отвечает ли дрон по MAVLink (heartbeat).
        /// Периодически отправляет heartbeat и ждёт любого MAVLink-пакета (v1 или v2).
        /// </summary>
        public static async Task<bool> CheckConnectionAsync(string host, int port, int timeoutMs = 5000)
        {
            try
            {
                using (var client = new UdpClient())
                {
                    client.Client.ReceiveTimeout = 500;
                    client.Connect(host, port);

                    var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                    DateTime nextHeartbeat = DateTime.MinValue;
                    byte seq = 0;

                    while (DateTime.UtcNow < deadline)
                    {
                        if (DateTime.UtcNow >= nextHeartbeat)
                        {
                            byte[] heartbeat = BuildHeartbeatPacket(seq++, 255, 190);
                            client.Send(heartbeat, heartbeat.Length);
                            nextHeartbeat = DateTime.UtcNow.AddMilliseconds(1000);
                        }

                        if (client.Available > 0)
                        {
                            byte[] received = client.Receive(ref remoteEp);
                            if (received != null && received.Length >= 6 &&
                                (received[0] == MAVLINK_V1_STX || received[0] == MAVLINK_V2_STX))
                                return true;
                        }
                        await Task.Delay(DelaySettings.MavLink_CheckConnectionPoll);
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
