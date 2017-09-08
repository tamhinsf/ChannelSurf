namespace ChannelSurfCli.Models
{
    public class Slack
    {
        public class Channels
        {
            public string channelId { get; set; }
            public string channelName { get; set; }
            public string channelDescription { get; set; }
        }

        public class Attachments
        {
            public string attachmentId { get; set; }
            public string attachmentUrl { get; set; }
            public string attachmentChannelId { get; set; }
            public string attachmentFileName {get;set;}
        }


    }
}
