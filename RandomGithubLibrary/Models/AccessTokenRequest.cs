using System;
using System.Collections.Generic;
using System.Text;

namespace RandomGithubLibrary.Models
{
    class AccessTokenRequest
    {
        public string Client_id { get; set; }
        public string Client_secret { get; set; }
        public string Code { get; set; }
        public string Redirect_url { get; set; }
        public string State { get; set; }
    }
}
