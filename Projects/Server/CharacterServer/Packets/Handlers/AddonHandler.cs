﻿/*
 * Copyright (C) 2012-2014 Arctium Emulation <http://arctium.org>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.IO;
using System.IO.Compression;
using CharacterServer.Network;
using CharacterServer.Packets.Server.Misc;
using CharacterServer.Packets.Structures.Misc;
using Framework.Constants.Misc;
using Framework.Logging;
using Framework.Network.Packets;

namespace CharacterServer.Packets.Handlers
{
    class AddonHandler
    {
        static readonly byte[] addonPublicKey =
        {
            0xC3, 0x5B, 0x50, 0x84, 0xB9, 0x3E, 0x32, 0x42, 0x8C, 0xD0, 0xC7, 0x48, 0xFA, 0x0E, 0x5D, 0x54,
            0x5A, 0xA3, 0x0E, 0x14, 0xBA, 0x9E, 0x0D, 0xB9, 0x5D, 0x8B, 0xEE, 0xB6, 0x84, 0x93, 0x45, 0x75,
            0xFF, 0x31, 0xFE, 0x2F, 0x64, 0x3F, 0x3D, 0x6D, 0x07, 0xD9, 0x44, 0x9B, 0x40, 0x85, 0x59, 0x34,
            0x4E, 0x10, 0xE1, 0xE7, 0x43, 0x69, 0xEF, 0x7C, 0x16, 0xFC, 0xB4, 0xED, 0x1B, 0x95, 0x28, 0xA8,
            0x23, 0x76, 0x51, 0x31, 0x57, 0x30, 0x2B, 0x79, 0x08, 0x50, 0x10, 0x1C, 0x4A, 0x1A, 0x2C, 0xC8,
            0x8B, 0x8F, 0x05, 0x2D, 0x22, 0x3D, 0xDB, 0x5A, 0x24, 0x7A, 0x0F, 0x13, 0x50, 0x37, 0x8F, 0x5A,
            0xCC, 0x9E, 0x04, 0x44, 0x0E, 0x87, 0x01, 0xD4, 0xA3, 0x15, 0x94, 0x16, 0x34, 0xC6, 0xC2, 0xC3,
            0xFB, 0x49, 0xFE, 0xE1, 0xF9, 0xDA, 0x8C, 0x50, 0x3C, 0xBE, 0x2C, 0xBB, 0x57, 0xED, 0x46, 0xB9,
            0xAD, 0x8B, 0xC6, 0xDF, 0x0E, 0xD6, 0x0F, 0xBE, 0x80, 0xB3, 0x8B, 0x1E, 0x77, 0xCF, 0xAD, 0x22,
            0xCF, 0xB7, 0x4B, 0xCF, 0xFB, 0xF0, 0x6B, 0x11, 0x45, 0x2D, 0x7A, 0x81, 0x18, 0xF2, 0x92, 0x7E,
            0x98, 0x56, 0x5D, 0x5E, 0x69, 0x72, 0x0A, 0x0D, 0x03, 0x0A, 0x85, 0xA2, 0x85, 0x9C, 0xCB, 0xFB,
            0x56, 0x6E, 0x8F, 0x44, 0xBB, 0x8F, 0x02, 0x22, 0x68, 0x63, 0x97, 0xBC, 0x85, 0xBA, 0xA8, 0xF7,
            0xB5, 0x40, 0x68, 0x3C, 0x77, 0x86, 0x6F, 0x4B, 0xD7, 0x88, 0xCA, 0x8A, 0xD7, 0xCE, 0x36, 0xF0,
            0x45, 0x6E, 0xD5, 0x64, 0x79, 0x0F, 0x17, 0xFC, 0x64, 0xDD, 0x10, 0x6F, 0xF3, 0xF5, 0xE0, 0xA6,
            0xC3, 0xFB, 0x1B, 0x8C, 0x29, 0xEF, 0x8E, 0xE5, 0x34, 0xCB, 0xD1, 0x2A, 0xCE, 0x79, 0xC3, 0x9A,
            0x0D, 0x36, 0xEA, 0x01, 0xE0, 0xAA, 0x91, 0x20, 0x54, 0xF0, 0x72, 0xD8, 0x1E, 0xC7, 0x89, 0xD2
        };

        public static byte[] GetAddonInfoData(CharacterSession session, byte[] packedData, int packedSize, int unpackedSize)
        {
            // Check ZLib header (normal mode)
            if (packedData[0] == 0x78 && packedData[1] == 0x9C)
            {
                var unpackedAddonData = new byte[unpackedSize];

                if (packedSize > 0)
                {
                    using (var inflate = new DeflateStream(new MemoryStream(packedData, 2, packedSize - 6), CompressionMode.Decompress))
                    {
                        var decompressed = new MemoryStream();
                        inflate.CopyTo(decompressed);

                        decompressed.Seek(0, SeekOrigin.Begin);

                        for (int i = 0; i < unpackedSize; i++)
                            unpackedAddonData[i] = (byte)decompressed.ReadByte();
                    }
                }

                return unpackedAddonData;
            }
            else
            {
                Log.Message(LogType.Error, "Wrong AddonInfo for Client '{0}'.", session.GetClientInfo());

                session.Dispose();
            }

            return null;
        }

        //! TODO Implement server side addon & banned addon handling
        public static void HandleAddonInfo(CharacterSession session, byte[] addonData)
        {
            var addonInfo = new AddonInfo();
            var addonDataReader = new Packet(addonData, 0);

            var addons = addonDataReader.Read<uint>();

            for (var i = 0; i < addons; i++)
            {
                var addonName         = addonDataReader.Read<string>(0, true);
                var addonInfoProvided = addonDataReader.Read<bool>();
                var addonCRC          = addonDataReader.Read<uint>();
                var urlCRC            = addonDataReader.Read<uint>();

                Log.Message(LogType.Debug, "AddonData: Name '{0}', Info Provided '{1}', CRC '0x{2:X}', URL CRC '0x{3:X}'.",
                            addonName, addonInfoProvided, addonCRC, urlCRC);

                addonInfo.Addons.Add(new AddonInfoData
                {
                    InfoProvided = addonInfoProvided,
                    KeyProvided  = true,
                    KeyData      = addonPublicKey
                });
            }

            session.Send(addonInfo);
        }
    }
}
