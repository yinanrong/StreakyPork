using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sp.Settle.Utility
{
    public class SecretUtil
    {
        #region Hash

        public static string GetMd5(string str, string salt = null)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{str}:{salt}"));
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpperInvariant();
            }
        }

        public static string GetSha256(string str)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        #endregion

        #region RSA

        public static string RsaSign256(string data, string privateKey)
        {
            var rsaCsp = DecodeRsaPrivateKey(Convert.FromBase64String(privateKey));
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsaCsp.SignData(dataBytes, HashAlgorithmName.SHA256.Name);
            return Convert.ToBase64String(signatureBytes);
        }

        public static string RsaSign1(RSA provider, string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = provider.SignData(dataBytes, 0, dataBytes.Length, HashAlgorithmName.SHA1,
                RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }

        public static bool RsaVerify256(string data, string sign, string publicKey)
        {
            var rsaCsp = DecodeRsaPublicKey(publicKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signBytes = Convert.FromBase64String(sign);
            return rsaCsp.VerifyData(dataBytes, signBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public static bool RsaVerify1(RSA provider, string content, string sign)
        {
            var data = Convert.FromBase64String(sign);
            return provider.VerifyData(Encoding.UTF8.GetBytes(content), data, HashAlgorithmName.SHA1,
                RSASignaturePadding.Pkcs1);
        }

        public static bool RsaVerify1(string content, string sign, byte[] x509Key)
        {
            var data = Convert.FromBase64String(sign);
            var provider = DecodeX509PublicKey(x509Key);
            return provider.VerifyData(Encoding.UTF8.GetBytes(content), data, HashAlgorithmName.SHA1,
                RSASignaturePadding.Pkcs1);
        }
        private static RSA DecodeX509PublicKey(byte[] x509Key)
        {
            byte[] seqOid = { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };

            var ms = new MemoryStream(x509Key);
            var reader = new BinaryReader(ms);

            if (reader.ReadByte() == 0x30)
                ReadAsnLength(reader); //skip the size
            else
                return null;

            int identifierSize; //total length of Object Identifier section
            if (reader.ReadByte() == 0x30)
                identifierSize = ReadAsnLength(reader);
            else
                return null;

            if (reader.ReadByte() == 0x06) //is the next element an object identifier?
            {
                var oidLength = ReadAsnLength(reader);
                var oidBytes = new byte[oidLength];
                reader.Read(oidBytes, 0, oidBytes.Length);
                if (oidBytes.SequenceEqual(seqOid) == false) //is the object identifier rsaEncryption PKCS#1?
                    return null;

                var remainingBytes = identifierSize - 2 - oidBytes.Length;
                reader.ReadBytes(remainingBytes);
            }

            if (reader.ReadByte() == 0x03) //is the next element a bit string?
            {
                ReadAsnLength(reader); //skip the size
                reader.ReadByte(); //skip unused bits indicator
                if (reader.ReadByte() == 0x30)
                {
                    ReadAsnLength(reader); //skip the size
                    if (reader.ReadByte() == 0x02) //is it an integer?
                    {
                        var modulusSize = ReadAsnLength(reader);
                        var modulus = new byte[modulusSize];
                        reader.Read(modulus, 0, modulus.Length);
                        if (modulus[0] == 0x00) //strip off the first byte if it's 0
                        {
                            var tempModulus = new byte[modulus.Length - 1];
                            Array.Copy(modulus, 1, tempModulus, 0, modulus.Length - 1);
                            modulus = tempModulus;
                        }

                        if (reader.ReadByte() == 0x02) //is it an integer?
                        {
                            var exponentSize = ReadAsnLength(reader);
                            var exponent = new byte[exponentSize];
                            reader.Read(exponent, 0, exponent.Length);

                            var rsa = RSA.Create();
                            var rsaKeyInfo = new RSAParameters
                            {
                                Modulus = modulus,
                                Exponent = exponent
                            };
                            rsa.ImportParameters(rsaKeyInfo);
                            return rsa;
                        }
                    }
                }
            }
            return null;
        }

        private static RSACryptoServiceProvider DecodeRsaPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = {0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00};

            var x509Key = Convert.FromBase64String(publicKeyString);

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            using (var mem = new MemoryStream(x509Key))
            {
                using (var binr = new BinaryReader(mem)) //wrap Memory Stream with BinaryReader for easy reading
                {
                    byte bt = 0;
                    ushort twobytes = 0;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte(); //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16(); //advance 2 bytes
                    else
                        return null;

                    var seq = binr.ReadBytes(15);
                    if (!CompareBytearrays(seq, seqOid)) //make sure Sequence for OID is correct
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8103
                    ) //data read as little endian order (actual data order for Bit String is 03 81)
                        binr.ReadByte(); //advance 1 byte
                    else if (twobytes == 0x8203)
                        binr.ReadInt16(); //advance 2 bytes
                    else
                        return null;

                    bt = binr.ReadByte();
                    if (bt != 0x00) //expect null byte next
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte(); //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16(); //advance 2 bytes
                    else
                        return null;

                    twobytes = binr.ReadUInt16();
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;

                    if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                    {
                        lowbyte = binr.ReadByte(); // read next bytes which is bytes in modulus
                    }
                    else if (twobytes == 0x8202)
                    {
                        highbyte = binr.ReadByte(); //advance 2 bytes
                        lowbyte = binr.ReadByte();
                    }
                    else
                    {
                        return null;
                    }

                    byte[] modint =
                        {lowbyte, highbyte, 0x00, 0x00}; //reverse byte order since asn.1 key uses big endian order
                    var modsize = BitConverter.ToInt32(modint, 0);

                    var firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {
                        //if first byte (highest order) of modulus is zero, don't include it
                        binr.ReadByte(); //skip this null byte
                        modsize -= 1; //reduce modulus buffer size by 1
                    }

                    var modulus = binr.ReadBytes(modsize); //read the modulus bytes

                    if (binr.ReadByte() != 0x02) //expect an Integer for the exponent data
                        return null;
                    var expbytes =
                        binr.ReadByte(); // should only need one byte for actual exponent data (for all useful values)
                    var exponent = binr.ReadBytes(expbytes);

                    // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                    var cspParameters = new CspParameters {Flags = CspProviderFlags.UseMachineKeyStore};
                    var rsa = new RSACryptoServiceProvider(2048, cspParameters);
                    var rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);

                    return rsa;
                }
            }
        }

        private static RSACryptoServiceProvider DecodeRsaPrivateKey(byte[] privkey)
        {
            using (var stream = new MemoryStream(privkey))
            {
                using (var binr = new BinaryReader(stream)) //wrap Memory Stream with BinaryReader for easy reading
                {
                    try
                    {
                        var twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8130)
                            //data read as little endian order (actual data order for Sequence is 30 81)
                            binr.ReadByte(); //advance 1 byte
                        else if (twobytes == 0x8230)
                            binr.ReadInt16(); //advance 2 bytes
                        else
                            return null;

                        twobytes = binr.ReadUInt16();
                        if (twobytes != 0x0102) //version number
                            return null;
                        var bt = binr.ReadByte();
                        if (bt != 0x00)
                            return null;
                        //------  all private key components are Integer sequences ----
                        var elems = GetIntegerSize(binr);
                        var modulus = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var e = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var d = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var p = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var q = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var dp = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var dq = binr.ReadBytes(elems);
                        elems = GetIntegerSize(binr);
                        var iq = binr.ReadBytes(elems);
                        var cspParameters = new CspParameters {Flags = CspProviderFlags.UseMachineKeyStore};
                        var rsa = new RSACryptoServiceProvider(2048, cspParameters);
                        var rsaParams = new RSAParameters
                        {
                            Modulus = modulus,
                            Exponent = e,
                            D = d,
                            P = p,
                            Q = q,
                            DP = dp,
                            DQ = dq,
                            InverseQ = iq
                        };
                        rsa.ImportParameters(rsaParams);
                        return rsa;
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("证书错误", e);
                    }
                }
            }
        }

        private static int ReadAsnLength(BinaryReader reader)
        {
            //Note: this method only reads lengths up to 4 bytes long as
            //this is satisfactory for the majority of situations.
            int length = reader.ReadByte();
            if ((length & 0x00000080) == 0x00000080) //is the length greater than 1 byte
            {
                var count = length & 0x0000000f;
                var lengthBytes = new byte[4];
                reader.Read(lengthBytes, 4 - count, count);
                Array.Reverse(lengthBytes); //
                length = BitConverter.ToInt32(lengthBytes, 0);
            }
            return length;
        }
        private static int GetIntegerSize(BinaryReader binr)
        {
            int count;
            int bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            switch (bt)
            {
                case 0x81:
                    count = binr.ReadByte();
                    break;
                case 0x82:
                    var highbyte = binr.ReadByte();
                    var lowbyte = binr.ReadByte();
                    byte[] modint = {lowbyte, highbyte, 0x00, 0x00};
                    count = BitConverter.ToInt32(modint, 0);
                    break;
                default:
                    count = bt;
                    break;
            }

            while (binr.ReadByte() == 0x00)
                count -= 1;
            binr.BaseStream.Seek(-1, SeekOrigin.Current); //last ReadByte wasn't a removed zero, so back up a byte
            return count;
        }

        private static bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            var i = 0;
            foreach (var c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }

            return true;
        }

        #endregion

        #region Des3

        private const int MaxMsgLength = 16 * 1024;
        private static readonly byte[] Iv = {1, 2, 3, 4, 5, 6, 7, 8};

        public static string Des3EncryptEcb(byte[] key, string data)
        {
            var resultByte = InitResultByteArray(data);
            var desdata = Des3EncodeEcb(key, Iv, resultByte);
            return ByteToHexStr(desdata);
        }

        public static string Des3DecryptEcb(byte[] key, string data)
        {
            var hexSourceData = Hex2Byte(data);
            var unDesResult = Des3DecodeEcb(key, Iv, hexSourceData);
            var dataSizeByte = new byte[4];
            dataSizeByte[0] = unDesResult[0];
            dataSizeByte[1] = unDesResult[1];
            dataSizeByte[2] = unDesResult[2];
            dataSizeByte[3] = unDesResult[3];
            var dsb = ByteArrayToInt(dataSizeByte, 0);
            if (dsb > MaxMsgLength) throw new Exception("msg over MAX_MSG_LENGTH or msg error");

            var tempData = new byte[dsb];
            for (var i = 0; i < dsb; i++) tempData[i] = unDesResult[4 + i];

            var hexStr = ByteToHexStr(tempData);
            var str = Hex2Bin(hexStr);
            return str;
        }

        public static string Des3EncryptCbc(byte[] key, string data)
        {
            var desdata = Des3EncodeCbc(key, Iv, Encoding.UTF8.GetBytes(data));
            return ByteToHexStr(desdata);
        }

        public static string Des3DecryptCbc(byte[] key, string data)
        {
            var desdata = Des3DecodeCbc(key, Iv, Encoding.UTF8.GetBytes(data));
            return Encoding.UTF8.GetString(desdata);
        }


        public static byte[] Hex2Byte(string b)
        {
            if (b.Length % 2 != 0) throw new Exception("长度不是偶数");

            var b2 = new byte[b.Length / 2];
            for (var n = 0; n < b.Length; n += 2)
            {
                var item = b.Substring(n, 2);
                // 两位一组，表示一个字节,把这样表示的16进制字符串，还原成一个进制字节
                b2[n / 2] = (byte) Convert.ToInt32(item, 16);
            }

            return b2;
        }


        public static string Hex2Bin(string hex)
        {
            const string digital = "0123456789abcdef";
            var hex2Char = hex.ToCharArray();
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var temp = digital.IndexOf(hex2Char[2 * i]) * 16;
                temp += digital.IndexOf(hex2Char[2 * i + 1]);
                bytes[i] = (byte) (temp & 0xff);
            }

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        ///     将数组转换成16进制字符串
        /// </summary>
        /// <returns></returns>
        public static string ByteToHexStr(byte[] bytes)
        {
            var returnStr = "";
            if (bytes != null) returnStr = bytes.Aggregate(returnStr, (current, t) => current + t.ToString("X2"));
            return returnStr.ToLower();
        }

        /// <summary>
        ///     字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] StrToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if (hexString.Length % 2 != 0)
                hexString += " ";
            var returnBytes = new byte[hexString.Length / 2];
            for (var i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        public static string BytesToString(byte[] src)
        {
            const string hexString = "0123456789ABCDEF";
            var stringBuilder = new StringBuilder();
            if (src == null || src.Length <= 0) return null;

            foreach (var t in src)
            {
                var v = t & 0xFF;
                var hv = Convert.ToString(v, 16);
                if (hv.Length < 2) stringBuilder.Append(0);

                stringBuilder.Append(hv);
            }

            var srcStr = stringBuilder.ToString();
            var chars = srcStr.ToCharArray();
            var bytes = new byte[srcStr.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var temp = hexString.IndexOf(chars[2 * i]) << 4;
                temp += hexString.IndexOf(chars[2 * i + 1]);
                bytes[i] = (byte) (temp & 0xff);
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private static byte[] InitResultByteArray(string data)
        {
            var source = Encoding.UTF8.GetBytes(data);
            var merchantData = source.Length;
            var x = (merchantData + 4) % 8;
            var y = x == 0 ? 0 : 8 - x;
            var resultByte = new byte[merchantData + 4 + y];
            resultByte[0] = (byte) ((merchantData >> 24) & 0xFF);
            resultByte[1] = (byte) ((merchantData >> 16) & 0xFF);
            resultByte[2] = (byte) ((merchantData >> 8) & 0xFF);
            resultByte[3] = (byte) (merchantData & 0xFF);
            //4.填充补位数据
            for (var i = 0; i < merchantData; i++) resultByte[4 + i] = source[i];

            for (var i = 0; i < y; i++) resultByte[merchantData + 4 + i] = 0x00;

            return resultByte;
        }

        private static int ByteArrayToInt(byte[] b, int offset)
        {
            var value = 0;
            for (var i = 0; i < 4; i++)
            {
                var shift = (4 - 1 - i) * 8;
                value += (b[i + offset] & 0x000000FF) << shift; //往高位游
            }

            return value;
        }

        #region CBC模式**

        /// <summary>
        ///     DES3 CBC模式加密
        /// </summary>
        /// <param name="key">密钥</param>
        /// <param name="iv">IV</param>
        /// <param name="data">明文的byte数组</param>
        /// <returns>密文的byte数组</returns>
        private static byte[] Des3EncodeCbc(byte[] key, byte[] iv, byte[] data)
        {
            var mStream = new MemoryStream();
            var tdsp = new TripleDESCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            var cStream = new CryptoStream(mStream, tdsp.CreateEncryptor(key, iv), CryptoStreamMode.Write);
            cStream.Write(data, 0, data.Length);
            cStream.FlushFinalBlock();
            var ret = mStream.ToArray();
            cStream.Close();
            mStream.Close();
            return ret;
        }

        /// <summary>
        ///     DES3 CBC模式解密
        /// </summary>
        /// <param name="key">密钥</param>
        /// <param name="iv">IV</param>
        /// <param name="data">密文的byte数组</param>
        /// <returns>明文的byte数组</returns>
        private static byte[] Des3DecodeCbc(byte[] key, byte[] iv, byte[] data)
        {
            var msDecrypt = new MemoryStream(data);
            var tdsp = new TripleDESCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            var csDecrypt = new CryptoStream(msDecrypt, tdsp.CreateDecryptor(key, iv), CryptoStreamMode.Read);
            var fromEncrypt = new byte[data.Length];
            csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
            return fromEncrypt;
        }

        #endregion

        #region ECB模式

        /// <summary>
        ///     DES3 ECB模式加密
        /// </summary>
        /// <param name="key">密钥</param>
        /// <param name="iv">IV(当模式为ECB时，IV无用)</param>
        /// <param name="data">明文的byte数组</param>
        /// <returns>密文的byte数组</returns>
        private static byte[] Des3EncodeEcb(byte[] key, byte[] iv, byte[] data)
        {
            var mStream = new MemoryStream();
            var tdsp = new TripleDESCryptoServiceProvider
            {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros
            };
            var cStream = new CryptoStream(mStream, tdsp.CreateEncryptor(key, iv), CryptoStreamMode.Write);
            cStream.Write(data, 0, data.Length);
            cStream.FlushFinalBlock();
            var ret = mStream.ToArray();
            cStream.Close();
            mStream.Close();
            return ret;
        }

        /// <summary>
        ///     DES3 ECB模式解密
        /// </summary>
        /// <param name="key">密钥</param>
        /// <param name="iv">IV(当模式为ECB时，IV无用)</param>
        /// <param name="data">密文的byte数组</param>
        /// <returns>明文的byte数组</returns>
        private static byte[] Des3DecodeEcb(byte[] key, byte[] iv, byte[] data)
        {
            var msDecrypt = new MemoryStream(data);
            var tdsp = new TripleDESCryptoServiceProvider
            {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros
            };
            var csDecrypt = new CryptoStream(msDecrypt, tdsp.CreateDecryptor(key, iv), CryptoStreamMode.Read);
            var fromEncrypt = new byte[data.Length];
            csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
            return fromEncrypt;
        }

        #endregion

        #endregion

        #region Rc4
        internal static class Rc4
        {
            public static string Encrypt(string key, string data)
            {
                var unicode = Encoding.GetEncoding("GBK");

                return Convert.ToBase64String(Encrypt(unicode.GetBytes(key), unicode.GetBytes(data)));
            }

            public static string Decrypt(string key, string data)
            {
                var unicode = Encoding.GetEncoding("GBK");

                return unicode.GetString(Encrypt(unicode.GetBytes(key), Convert.FromBase64String(data)));
            }

            public static byte[] Encrypt(byte[] key, byte[] data)
            {
                return EncryptOutput(key, data).ToArray();
            }

            public static byte[] Decrypt(byte[] key, byte[] data)
            {
                return EncryptOutput(key, data).ToArray();
            }

            private static byte[] EncryptInitalize(byte[] key)
            {
                var s = Enumerable.Range(0, 256)
                    .Select(i => (byte)i)
                    .ToArray();

                for (int i = 0, j = 0; i < 256; i++)
                {
                    j = (j + key[i % key.Length] + s[i]) & 255;

                    Swap(s, i, j);
                }

                return s;
            }

            private static IEnumerable<byte> EncryptOutput(byte[] key, IEnumerable<byte> data)
            {
                var s = EncryptInitalize(key);

                var i = 0;
                var j = 0;

                return data.Select(b =>
                {
                    i = (i + 1) & 255;
                    j = (j + s[i]) & 255;

                    Swap(s, i, j);

                    return (byte)(b ^ s[(s[i] + s[j]) & 255]);
                });
            }

            private static void Swap(byte[] s, int i, int j)
            {
                var c = s[i];
                s[i] = s[j];
                s[j] = c;
            }
        }
        #endregion
    }
}