using System;
using System.Collections.Generic;
using System.Text;

namespace RandomGithubLibrary.Models
{
    class AccessTokenRequest
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string code { get; set; }
        public string redirect_url { get; set; }
        public string state { get; set; }
    }
}
