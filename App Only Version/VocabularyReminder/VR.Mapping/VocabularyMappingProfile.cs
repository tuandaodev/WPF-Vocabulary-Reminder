using AutoMapper;
using System;
using VR.Domain.Models;
using VR.Dto;

namespace VR.Mapping
{
    public class VocabularyMappingProfile : Profile
    {
        public VocabularyMappingProfile()
        {
            CreateMap<Vocabulary, VocabularyDisplayDto>()
                .ForMember(dest => dest.NextReviewDateDisplay, opt => opt.MapFrom(src =>
                    src.NextReviewDate.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(src.NextReviewDate.Value).LocalDateTime.ToString("yyyy-MM-dd HH:mm")
                        : ""));
        }
    }
}