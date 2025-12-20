using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicStore.WebApi.Domain.Entities
{
    public class UserToken :  BaseEntity
    {
        public int UserId { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpiredDateAccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string codeRefreshTooken { get; set; }
        public DateTime ExpiredDateRefreshToken { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool isActive { get; set; }

    }
}
