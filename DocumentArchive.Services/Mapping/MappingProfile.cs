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
        // ========== Document ==========
        CreateMap<CreateDocumentDto, Document>();
        CreateMap<UpdateDocumentDto, Document>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UpdateBulkDocumentDto, Document>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<Document, DocumentResponseDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : null));
        CreateMap<Document, DocumentListItemDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "Без категории"));
        CreateMap<UpdateBulkDocumentDto, Document>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        

        // ========== User ==========
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<User, UserResponseDto>();
        CreateMap<User, UserListItemDto>();

        // ========== Category ==========
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<Category, CategoryResponseDto>();
        CreateMap<Category, CategoryListItemDto>();

        // ========== ArchiveLog ==========
        CreateMap<CreateArchiveLogDto, ArchiveLog>();
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