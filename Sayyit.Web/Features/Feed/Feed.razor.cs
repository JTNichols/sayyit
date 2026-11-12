using Microsoft.AspNetCore.Components;
using PostContract = Sayyit.Contracts.Content.Posts.Post;

namespace Sayyit.Web.Features.Feed;

public partial class Feed
{
    [Parameter, EditorRequired]
    public IReadOnlyList<PostContract> Posts { get; set; } = [];
}
