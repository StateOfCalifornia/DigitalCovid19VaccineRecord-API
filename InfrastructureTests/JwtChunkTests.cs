using Infrastructure;
using System;
using System.Collections.Generic;
using Xunit;

namespace InfrastructureTests
{
    public class JwtChunkTests
    {

        private readonly string example_00_d_jws = "eyJ6aXAiOiJERUYiLCJhbGciOiJFUzI1NiIsImtpZCI6IjNLZmRnLVh3UC03Z1h5eXd0VWZVQUR3QnVtRE9QS01ReC1pRUxMMTFXOXMifQ.3ZJLj9MwFIX_yuiyTZM4hKmaHR0kHguExMAGdeE6t42RH5HtRFNG-e_c63YkkKazYkV2Nz7-fM6xH0HHCB0MKY2xq6poZUgDSpOGUsnQxwofpB0NxoqEEwYowO0P0InbRrRtW7evy_XtpoBZQfcI6TQidD-u416dhxUPhLqu09ZOTv-SSXv3olD5WfdiA7sCVMAeXdLSfJ32P1EltnQYdPiOITKng7asS0E8_rudXG-QNQGjn4LC-2wfLgvFJQ4obwzRzk7ogHCijESejPkWDAme9nc1CZ6GZ8BfKA7t5w6lxTNEWm2IB28daULMZxz1jI57_OQHnrcl7BYKuNcU_p1MzBKbN2JVi1VTw7IUz7oRL7v5-HfFMck0xRyXLzwhX9AsldIO73yfCcr32h2z8XiKCe3l6dDNDGZd-nCsuNkq6r5S8wMBVN4JTb2GZbcUMF4qyHYOGNCxtz8bJJFXagp5icPea3tGNDlwzbGoqoMPlt4je5Eq-cDIXsfRyFzn9u7mPToM0tx88HHUSRoqiko0Pn2e7J63Qp0_cbXB5r9ssNn86wbXvLDQ9xs.xsivFoyYkN1j9YwtakRRTuS4X59xnqxhZWhBeio1GGdzrXlQg71KH4YDNOFVql85-narPFgOQjZ_b2sivwvcTA";

        private readonly string example_01_d_jws = "eyJ6aXAiOiJERUYiLCJhbGciOiJFUzI1NiIsImtpZCI6ImJWS1RuUndWcTRZVTlvTHd3U2hZRUxuUnRLb3BfTXNDQWpOa2xvd1llbWcifQ.3ZJLb9swEIT_SrC9ynpFcWPdEhdo2kNRoGkvgQ80tbYY8CGQlBA30H_vLu0ADRDn1FN1ozj7cWbIZ1AhQAt9jENoiyIY4WOPQsc-l8J3ocAnYQaNoSDhiB4ysNsdtNWyrpqmKZvLfHl1lcEkoX2GeBgQ2ofzuA_HxYIXhDqvU8aMVv0WUTn7rlC6SXXVCjYZSI8d2qiE_jFuH1FGtrTrlf-FPjCnhSYv84p4_Pd2tJ1G1ngMbvQS75N9OG1kpzggndZEOzqhA_yBMhJ51Pqn1yR4mW9LErws3gB_pzg0zx0Kg0eIMEoTD24saXxIZ-zVhJZ7_Cos-1jnsJkp4FZR-E8iMqtaLatFWS3qEuY5e9NN9b6bL68rDlHEMaS4fOER-YImIaWyuHZdIkjXKbtPxsMhRDSnp0M30-uPufP7gpstguoKOT0RQKZJqMtrmDdzBsOpgmRnhx4te_u7QRI5KUeftjjsvTJHRJ0ClxyLqto5b-g9shcho_OM7FQYtEh13q4vPqNFL_TFnQuDikJTUVSidvHbaLY8CmX66rMN1v9lg_XqXzd4zRszfX8A.dAAv2dHxSwwHS_U1rkVnLWy7VdJr169_6KaMy5h66mIEnihv-YYZVcTsaH-G1oq3aSGJW5r3sObgKGo_oYlcMQ";


        [Fact]
        public void ShouldGiveOneifLessThan1195_0()
        {
            var jwtChunk = new JwtChunk();
            var res = jwtChunk.Chunk(example_00_d_jws);
            var res2 = jwtChunk.Combine(res);
            Assert.Equal(example_00_d_jws, res2);
        }

        [Fact]
        public void ShouldGiveOneifLessThan1195_1()
        {
            var jwtChunk = new JwtChunk();
            var chunks = jwtChunk.Chunk(example_01_d_jws);
            var recombined = jwtChunk.Combine(chunks);
            Assert.Equal(example_01_d_jws, recombined);
            var st = "Abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789Abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789";
            chunks = jwtChunk.Chunk(st);
            recombined = jwtChunk.Combine(chunks);
            Assert.Equal(st,recombined);
        }
    }
}
