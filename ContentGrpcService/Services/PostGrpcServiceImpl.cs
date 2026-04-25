using AutoMapper;
using ContentBLL.Services.Interfaces;
using Grpc.Core;
using IdeaFusion.Grpc.Posts;
using Microsoft.Extensions.Logging;

namespace GrpcService.Services;

public class PostGrpcServiceImpl : PostGrpcService.PostGrpcServiceBase
{
    private readonly IPostService _postService;
    private readonly ITagService _tagService;
    private readonly IMapper _mapper;
    private readonly ILogger<PostGrpcServiceImpl> _logger;
 
    public PostGrpcServiceImpl(
        IPostService postService,
        ITagService tagService,
        IMapper mapper,
        ILogger<PostGrpcServiceImpl> logger)
    {
        _postService = postService;
        _tagService  = tagService;
        _mapper      = mapper;
        _logger      = logger;
    }
 
    // ── GetPostsPaged ─────────────────────────────────────────────────────────
 
    public override async Task<PagedPostsResponse> GetPostsPaged(
        GetPostsPagedRequest request,
        ServerCallContext context)
    {
        try
        {
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize   = request.PageSize   > 0 ? request.PageSize   : 10;
 
            IEnumerable<ContentBLL.DTO.Post.PostDto> posts;
 
            // Фільтруємо по тегу якщо передано
            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                var tag = await _tagService.GetByNameAsync(request.Tag, context.CancellationToken);
                if (tag is null)
                {
                    return new PagedPostsResponse
                    {
                        TotalCount = 0,
                        PageNumber = pageNumber,
                        PageSize   = pageSize
                    };
                }
 
                posts = await _postService.GetByTagAsync(tag.TagId, context.CancellationToken);
            }
            else
            {
                posts = await _postService.GetAllAsync(context.CancellationToken);
            }
 
            // Фільтр по статусу
            if (!string.IsNullOrWhiteSpace(request.Status))
                posts = posts.Where(p => p.Status == request.Status);
 
            // Пошук по тексту
            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var search = request.SearchText.ToLower();
                posts = posts.Where(p =>
                    p.Title.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }
 
            // Сортування
            posts = request.OrderBy?.ToLower() switch
            {
                "popular"  => posts.OrderByDescending(p => p.LikesCount),
                "views"    => posts.OrderByDescending(p => p.ViewsCount),
                _          => posts.OrderByDescending(p => p.CreatedAt) // "newest" за замовчуванням
            };
 
            var postList  = posts.ToList();
            var totalCount = postList.Count;
            var paged      = postList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
 
            var response = new PagedPostsResponse
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };
            response.Items.AddRange(paged.Select(p => _mapper.Map<PostDto>(p)));
 
            _logger.LogInformation(
                "GetPostsPaged: returned {Count}/{Total} posts (page {Page}, tag: {Tag}, status: {Status})",
                paged.Count, totalCount, pageNumber,
                request.Tag    ?? "all",
                request.Status ?? "all");
 
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPostsPaged gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
 
    // ── GetPostById ───────────────────────────────────────────────────────────
 
    public override async Task<PostResponse> GetPostById(
        GetPostByIdRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PostId) || !int.TryParse(request.PostId, out var postId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid PostId is required."));
 
            var post = await _postService.GetByIdAsync(postId, context.CancellationToken);
 
            if (post is null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Post {request.PostId} not found."));
 
            _logger.LogInformation("GetPostById: returned post {PostId}", postId);
 
            return new PostResponse { Post = _mapper.Map<PostDto>(post) };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPostById gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
 
    // ── GetPostsByAuthor ──────────────────────────────────────────────────────
 
    public override async Task<PagedPostsResponse> GetPostsByAuthor(
        GetPostsByAuthorRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId) || !int.TryParse(request.UserId, out var authorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid UserId is required."));
 
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize   = request.PageSize   > 0 ? request.PageSize   : 10;
 
            var posts = await _postService.GetByAuthorAsync(authorId, context.CancellationToken);
 
            if (!string.IsNullOrWhiteSpace(request.Status))
                posts = posts.Where(p => p.Status == request.Status);
 
            var postList   = posts.ToList();
            var totalCount = postList.Count;
            var paged      = postList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
 
            var response = new PagedPostsResponse
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };
            response.Items.AddRange(paged.Select(p => _mapper.Map<PostDto>(p)));
 
            _logger.LogInformation(
                "GetPostsByAuthor: returned {Count}/{Total} posts for author {AuthorId}",
                paged.Count, totalCount, authorId);
 
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPostsByAuthor gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
 
    // ── GetPostsByTeam ────────────────────────────────────────────────────────
 
    public override async Task<PagedPostsResponse> GetPostsByTeam(
        GetPostsByTeamRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TeamId) || !int.TryParse(request.TeamId, out var snapshotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid TeamId is required."));
 
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize   = request.PageSize   > 0 ? request.PageSize   : 10;
 
            var posts = await _postService.GetByCollaborationAsync(snapshotId, context.CancellationToken);
 
            var postList   = posts.ToList();
            var totalCount = postList.Count;
            var paged      = postList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
 
            var response = new PagedPostsResponse
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };
            response.Items.AddRange(paged.Select(p => _mapper.Map<PostDto>(p)));
 
            _logger.LogInformation(
                "GetPostsByTeam: returned {Count}/{Total} posts for team snapshot {SnapshotId}",
                paged.Count, totalCount, snapshotId);
 
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPostsByTeam gRPC call");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}