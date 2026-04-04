using AutoMapper;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for Booking-related mappings
/// </summary>
public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => (int)(src.EndTime - src.StartTime).TotalMinutes))
            .ForMember(dest => dest.IsUpcoming, opt => opt.MapFrom(src => src.StartTime > DateTime.UtcNow))
            .ForMember(dest => dest.IsPast, opt => opt.MapFrom(src => src.EndTime < DateTime.UtcNow));
    }
}
