using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CityInfo.API.Controllers
{
    // API Controller attribute. Not strictly necessary but it configures this controller with features and behaviour aimed at improving the experience of writing APIs.
    [ApiController]
    [Authorize]
    [Route("api/cities")]

    // ControllerBase contains basic functionality controllers need such as access to model state, current user, common methods for returning responses that implement IActionResult.
    // You can also user "Controller" but that contains extra functionality for views which we don't need.
    public class CitiesController : ControllerBase
    {
        //private readonly CitiesDataStore _citiesDataStore;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;
        const int maxCitiesPageSize = 20;

        //public CitiesController(CitiesDataStore citiesDataStore)
        //{
        //    _citiesDataStore = citiesDataStore;
        //}

        // We now inject the repository instead of the local data store:
        // We inject the contract and not the exact implementation. This is so that
        // - the controller is decoupled from the persistence implementation
        // - Flexibility to switch out the implementation
        // - Mock testing is easier
        // - Helps to abide by SRP by not directly interacting with persistence logic.

        public CitiesController(ICityInfoRepository cityInfoRepository, IMapper mapper)
        {
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        // FromQuery attribute added for readability purposes and can take an arguement to map query names...
        public async Task<ActionResult<IEnumerable<CityWithoutPoiDto>>> GetCities(
            [FromQuery] string? name, string? searchQuery, int pageNum = 1, int pageSize = 10)
        {
            if (pageSize > maxCitiesPageSize)
            {
                pageSize = maxCitiesPageSize;
            }
            var (cityEntities, paginationMetadata) = await _cityInfoRepository.GetCitiesAsync(name, searchQuery, pageNum, pageSize);

            // Return metadata in header to save bandwidth, provide cacheability and SOC.
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

            return Ok(_mapper.Map<IEnumerable<CityWithoutPoiDto>>(cityEntities));

            // Old code:
            //var results = new List<CityWIthoutPoiDto>();
            //foreach (var cityEntity in cityEntities)
            //{
            //    results.Add(new CityWIthoutPoiDto
            //    {
            //        Id = cityEntity.Id,
            //        Description = cityEntity.Description,
            //        Name = cityEntity.Name
            //    });
            //}
            //return Ok(results);
            //return Ok(_citiesDataStore.Cities);


        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCity(int id, bool includePoi = false)
        {
            //var cityToReturn = _citiesDataStore.Cities.FirstOrDefault(c => c.Id == id);

            //if (cityToReturn == null)
            //{
            //    return NotFound();
            //}

            var city = await _cityInfoRepository.GetCityAsync(id, includePoi);
            if (city == null)
            {
                return NotFound();
            }

            if (includePoi)
            {
                return Ok(_mapper.Map<CityDto>(city));
            }

            return Ok(_mapper.Map<CityWithoutPoiDto>(city));
        }
    }
}
