using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelSurfCli.ViewModels
{
    public class SimpleMessage
    {
        public string id {get;set;}
        public string user { get; set; }
        public string userId { get; set; }
        public string text { get; set; }
        public string editedByUser { get; set; }
        public string editedByUserId { get; set; }
        public string ts { get; set; }
        public List<Attachments> attachments { get; set; }
        public List<Reaction> reactions { get; set; }
        public FileAttachment fileAttachment { get; set; }

        public class FileAttachment
        {
            public string id { get; set; }
            public string originalName { get; set; }
            public string originalTitle { get; set; }
            public string originalUrl { get; set; }
            
            public string spoId {get;set;}
            public string spoUrl {get;set;}
        }

        public class Attachments
        {
            public string text { get; set; }

            public string service_name {get;set;}
            public string pretext { get; set; }
            public string color { get; set; } = "#D3D3D3";
            public string title { get; set; }
            public string title_link {get;set;}
            public string url {get;set;}
            
            public List<Fields> fields { get; set; }
            public class Fields
            {
                public string title { get; set; }
                public string value { get; set; }
                public bool shortWidth { get; set; } = false;
            }
        }

        public class Reaction
        {
            public string name { get; set; }
            public List<string> users { get; set; }
        }
    }
}
