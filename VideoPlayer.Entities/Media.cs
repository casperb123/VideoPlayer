using System;

namespace VideoPlayer.Entities
{
    public class Media
    {
        private string source;
        private Uri uri;

        public Uri Uri
        {
            get => uri;
        }

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
                    this.uri = uri;
                }
                else
                {
                    throw new ArgumentException($"{value} isn't a valid source");
                }

                source = value;
            }
        }

        public Media(string source)
        {
            Source = source;
        }
    }
}
