using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelSurfCli.ViewModels
{
    public class SimpleUser
    {
        public string userId { get; set; }
        public string name { get; set; }
        public string real_name { get; set; }
        public string email { get; set; }
        // public string image_filename { get; set; }
        public bool is_bot { get; set; } = false; 
    }
}
