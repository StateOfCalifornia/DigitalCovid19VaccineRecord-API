using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Infrastructure
{
    public class JwtChunk : IJwtChunk
    {
        private readonly int MAX_CHUNK_SIZE = (int)(1191);

        public List<string> Chunk(string jwt)
        {
            var charsPerChunk = Math.Ceiling(jwt.Length / Math.Ceiling((decimal)jwt.Length / MAX_CHUNK_SIZE));

            var numChunks = Math.Ceiling(jwt.Length / charsPerChunk);

            var shcs = new List<string>();
            for (var chunk = 0; chunk < numChunks; chunk++)
            {
                var shc = "";
                var min = Math.Min(charsPerChunk, jwt.Length - (int)(chunk * charsPerChunk));
                for (var i = 0; i < min; i++)
                {
                    var jwtChar = jwt.Substring((int)(chunk * charsPerChunk) + i, 1);
                    var shcCharInt = (int)jwtChar[0] - 45;
                    var shcChar = Convert.ToString(shcCharInt);
                    if (shcCharInt < 10)
                    {
                        shcChar = "0" + shcCharInt;
                    }
                    shc += shcChar;
                }
                if (numChunks > 1)
                {
                    shc = $"shc:/{chunk + 1}/{numChunks}/{shc}";
                }
                else
                {
                    shc = $"shc:/{shc}";
                }
                shcs.Add(shc);
            }
            if(shcs.Count > 1)
            {
                throw new ArgumentException("Too Many Chunks");
            }
            return shcs;
        }
        public string Combine(List<string> chunks)
        {

            var numChunks = chunks.Count;

            var stringBuilder = new StringBuilder();
            for (var inx = 0; inx < numChunks; inx++)
            {
                var chunk = chunks[inx];
                //"shc:/1/2/33050709"
                var chunkData = chunk.Split("/")[1];
                if (numChunks > 1)
                {
                    chunkData = chunk.Split("/")[3];
                }
                for (var i = 0; i < chunkData.Length; i = i + 2)
                {

                    var jwtCharOrd = Int32.Parse(chunkData.Substring(i, 2));
                    var jwtChar = jwtCharOrd + 45;
                    stringBuilder.Append((char)jwtChar);
                }
               
            }
            return stringBuilder.ToString(); ;
        }

    }
}
