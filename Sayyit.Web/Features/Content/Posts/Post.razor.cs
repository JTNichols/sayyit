using Microsoft.AspNetCore.Components;
using System.Globalization;
using PostContract = Sayyit.Contracts.Content.Posts.Post;

namespace Sayyit.Web.Features.Content.Posts
{
    public partial class Post
    {
        [Parameter, EditorRequired]
        public PostContract? PostModel { get; set; }

        private static DateTime AsUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

        private static string ToLocalDisplay(DateTime value) =>
            AsUtc(value)
                .ToLocalTime()
                .ToString("g", CultureInfo.CurrentCulture);

        private static string ToUtcIso(DateTime value) =>
            AsUtc(value).ToString("O", CultureInfo.InvariantCulture);
    }
}
