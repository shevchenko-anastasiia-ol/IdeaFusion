using AutoMapper;
using ContentBLL.DTO.Tag;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;

namespace ContentBLL.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
 
    public TagService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
 
    public async Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await _uow.TagRepository.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<TagDto>>(tags);
    }
 
    public async Task<TagDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var tag = await _uow.TagRepository.GetByIdAsync(id, ct);
        return tag is null ? null : _mapper.Map<TagDto>(tag);
    }
 
    public async Task<IEnumerable<TagDto>> GetByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var tags = await _uow.TagRepository.GetByPostIdAsync(postId, ct);
        return _mapper.Map<IEnumerable<TagDto>>(tags);
    }
 
    public async Task<TagDto> CreateAsync(TagCreateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.TagRepository.GetByNameAsync(dto.Name, ct);
        if (existing is not null)
            throw new BusinessConflictException($"Тег '{dto.Name}' вже існує.");
 
        var tag = _mapper.Map<Tag>(dto);
        await _uow.TagRepository.AddAsync(tag, ct);
 
        return _mapper.Map<TagDto>(tag);
    }
 
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.TagRepository.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Тег з id={id} не знайдено.");
 
        await _uow.TagRepository.DeleteAsync(existing.TagId, ct);
    }
}