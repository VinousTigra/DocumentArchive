using AutoMapper;
using DocumentArchive.Core.DTOs.ArchiveLog;
using DocumentArchive.Core.DTOs.Category;
using DocumentArchive.Core.DTOs.Document;
using DocumentArchive.Core.DTOs.User;
using DocumentArchive.Core.Models;

namespace DocumentArchive.Services.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //  Document
        CreateMap<CreateDocumentDto, Document>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore());

        CreateMap<UpdateDocumentDto, Document>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<UpdateBulkDocumentDto, Document>()
            .ForMember(dest => dest.UploadDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Document, DocumentResponseDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : null));

        CreateMap<Document, DocumentListItemDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Без категории"));

        // User
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForMember(dest => dest.Logs, opt => opt.Ignore());

        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForMember(dest => dest.Logs, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<User, UserResponseDto>();
        CreateMap<User, UserListItemDto>();

        // Category 
        CreateMap<CreateCategoryDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore());

        CreateMap<UpdateCategoryDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Category, CategoryResponseDto>();
        CreateMap<Category, CategoryListItemDto>();

        // ArchiveLog
        CreateMap<CreateArchiveLogDto, ArchiveLog>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Document, opt => opt.Ignore());

        CreateMap<ArchiveLog, ArchiveLogResponseDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
            .ForMember(dest => dest.DocumentTitle,
                opt => opt.MapFrom(src => src.Document != null ? src.Document.Title : null));

        CreateMap<ArchiveLog, ArchiveLogListItemDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : null));
    }
}