using System;

namespace Sayyit.Contracts.Content.Posts
{
    public class Post
    {
        public long PostId { get; set; }

        public Guid UserId { get; set; }

        public string Body { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; }

        public DateTime? UpdatedUtc { get; set; }

        public bool IsDeleted { get; set; }
    }
}
