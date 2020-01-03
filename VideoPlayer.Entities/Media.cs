using System;
using System.IO;

namespace VideoPlayer.Entities
{
    public class Media
    {
        private string source;

        public Uri Uri { get; private set; }

        public string Source
        {
            get { return source; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"{value} isn't a valid source");
                }
                if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                {
                    Uri = uri;
                }
                else
                {
                    throw new ArgumentException($"{value} isn't a valid source");
                }

                source = value;
            }
        }

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(source);
            }
        }

        public Media(string source)
        {
            Source = source;
        }
    }
}
