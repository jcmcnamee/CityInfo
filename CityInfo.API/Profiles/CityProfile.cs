using AutoMapper;

namespace CityInfo.API.Profiles
{
    // Here we want to is to create a map from the City entity to the CityWithoutPoiDto
    public class CityProfile : Profile
    {
        public CityProfile()
        {
            // AutoMapper will map properties on the source object the properties with the 
            // same names on the destination type.
            CreateMap<Entities.City, Models.CityWithoutPoiDto>();
            CreateMap<Entities.City, Models.CityDto>();
        }
    }
}
