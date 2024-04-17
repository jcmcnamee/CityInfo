using Asp.Versioning;
using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [Route("api/v{version:apiVersion}/cities/{cityId}/pointsofinterest")]
    [Authorize(Policy = "MustBeFromAntwerp")]
    [ApiVersion(2)]
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _mailService;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;
        //private readonly CitiesDataStore _citiesDataStore;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger, IMailService mailService, ICityInfoRepository cityInfoRepository, IMapper mapper)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _mailService = mailService ??
                throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ??
                throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            //_citiesDataStore = citiesDataStore ?? throw new ArgumentNullException(nameof(citiesDataStore));
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> GetPointsOfInterest(int cityId)
        {
            var cityName = User.Claims.FirstOrDefault(c => c.Type == "city")?.Value;

            if(!(await _cityInfoRepository.CityNameMatchesCityId(cityName, cityId)))
            {
                return Forbid();
            }

            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                _logger.LogInformation(
                    $"City with ID {cityId} wasn't found when accessing PoI.");
                return NotFound();
            }

            var pointsOfInterestForCity = await _cityInfoRepository.GetPointsOfInterestForCityAsync(cityId);

            return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity));


            // Old code:
            //try 
            //{
            //    var city = _citiesDataStore.Cities.FirstOrDefault(c => c.Id == cityId);

            //    if (city == null)
            //    {
            //    _logger.LogInformation($"City with id {cityId} was not found when accessing PoI.");
            //    return NotFound();
            //    }

            //    return Ok(city.PointsOfInterest);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogCritical($"Exception while getting PoI for city with id {cityId}", ex);
            //    return StatusCode(500, "A problem happened while handling your request");
            //}
        }

        [HttpGet("{pointofinterestid}", Name = "GetPointOfInterest")]
        public async Task<ActionResult<PointOfInterestDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterest = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterest == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest));

            // Old code:
            //var city = _citiesDataStore.Cities.FirstOrDefault(c => c.Id == cityId);

            //if (city == null)
            //{
            //    return NotFound();
            //}

            //var pointOfInterest = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);

            //if (pointOfInterest == null)
            //{
            //    return NotFound();
            //}

            //return Ok(pointOfInterest);

        }

        [HttpPost]
        // [FromBody] is not actually required
        public async Task<ActionResult<PointOfInterestDto>> CreatePointOfInterest(
            int cityId, [FromBody] PointOfInterestForCreationDto pointOfInterest)
        {
            // ModelState is a dictionary containing the state of the model (PointOfInterestForCreatioknDto), and model binding validation.
            // It represents a collection of name/value pairs that were submitted to our API one for each property, including error messages.
            // It checks the rules that we added to our model via attributes.

            // if (!ModelState.IsValid)
            // {
            //     return BadRequest();
            // }

            // Check
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // Map
            var finalPointOfInterest = _mapper.Map<Entities.PointOfInterest>(pointOfInterest);

            // Add
            await _cityInfoRepository.AddPointOfInterestForCityAsync(cityId, finalPointOfInterest);

            // Then save
            await _cityInfoRepository.SaveChangesAsync();

            // Map back to a DTO
            var createdPoiToReturn = _mapper.Map<Models.PointOfInterestDto>(finalPointOfInterest);

            // Return it 
            return CreatedAtRoute("GetPointOfInterest",
                new
                {
                    cityId = cityId,
                    PointOfInterestId = createdPoiToReturn.Id
                },
                createdPoiToReturn);


            // Old code:
            //var city = _citiesDataStore.Cities.FirstOrDefault(c => c.Id == cityId);
            //if (city == null)
            //{
            //    return NotFound();
            //}

            // Calculate the new ID of the new point of interest
            // Can now be removed as the PK is automatically generated....
            //var maxPointOfInterestId = _citiesDataStore.Cities.SelectMany(
            //    c => c.PointsOfInterest).Max(p => p.Id);

            // Remove previous mapping as we now use a mapper:
            //var finalPointOfInterest = new PointOfInterestDto()
            //{
            //    Id = ++maxPointOfInterestId,
            //    Name = pointOfInterest.Name,
            //    Description = pointOfInterest.Description
            //};

            // We now do not deal with the city entity at this level any more
            // So we can remove this:
            //city.PointsOfInterest.Add(finalPointOfInterest);

            //return CreatedAtRoute("GetPointOfInterest",
            //    new
            //    {
            //        cityId = cityId,
            //        PointOfInterestId = finalPointOfInterest.Id
            //    },
            //    finalPointOfInterest);


        }

        [HttpPut("{pointofinterestid}")]
        public async Task<ActionResult> UpdatePointOfInterest(int cityId, int pointOfInterestId, PointOfInterestForUpdateDto pointOfInterest)
        {
            // Method for doing full updates to resources:

            // Check if city exists
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // Check if resource to update exists
            var poiEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (poiEntity == null)
            {
                return NotFound();
            }

            // Map - with a second arguement the input overrides the destination.
            _mapper.Map(pointOfInterest, poiEntity);

            // Save
            await _cityInfoRepository.SaveChangesAsync();

            // Return
            return NoContent();

            // Old code:
            //var city = _citiesDataStore.Cities.FirstOrDefault(c => c.Id == cityId);
            //if (city == null)
            //{
            //    return NotFound();
            //}

            //// All fields must be updated by convention with a put request.
            //var pointOfInterestFromStore = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);
            //if (pointOfInterestFromStore == null)
            //{
            //    return NotFound();
            //}

            //pointOfInterestFromStore.Name = pointOfInterest.Name;
            //pointOfInterestFromStore.Description = pointOfInterest.Description;

            //return NoContent();

        }

        [HttpPatch("{pointofinterestid}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(
            int cityId, int pointOfInterestId, JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            // Check city
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // Get & check for PoI
            var poiEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (poiEntity == null)
            {
                return NotFound();
            }

            // Map entity to DTO
            var poiToPatch = _mapper.Map<PointOfInterestForUpdateDto>(poiEntity);

            // Apply the patch document to the poiToPatch DTO
            patchDocument.ApplyTo(poiToPatch, ModelState);

            // Validate
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            // Manual validation for non-DTO
            if (!TryValidateModel(poiToPatch))
            {
                return BadRequest(ModelState);
            }

            // Map changes back into the entity
            _mapper.Map(poiToPatch, poiEntity);

            // Save
            await _cityInfoRepository.SaveChangesAsync();

            return NoContent();





            // Old code:

            // Update a new DTO with the stored POI.
            // Now replaced by mapper code
            //var pointOfInterestToPatch = new PointOfInterestForUpdateDto()
            //{
            //    Name = pointOfInterestFromStore.Name,
            //    Description = pointOfInterestFromStore.Description
            //};

            // Remember: ModelState holds the validation data that we put on the DTO.
            // In this instance, the input is NOT a DTO, but rather a JSONPatchDocument.
            // That means we need to additionally add manual validation...
            //patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            //if (!ModelState.IsValid)
            //{
            //    return BadRequest();
            //}

            // Here is the manual validation
            //if (!TryValidateModel(pointOfInterestToPatch))
            //{
            //    return BadRequest(ModelState);
            //}

            //pointOfInterestFromStore.Name = pointOfInterestToPatch.Name;
            //pointOfInterestFromStore.Description = pointOfInterestToPatch.Description;

            //return NoContent();

        }

        [HttpDelete("{pointOfInterestId}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            // Check
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var poiEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (poiEntity == null)
            {
                return NotFound();
            }

            // Delete
            _cityInfoRepository.DeletePointOfInterest(poiEntity);

            // Save
            await _cityInfoRepository.SaveChangesAsync();

            // Send mail
            _mailService.Send("PoI deleted.", $"PoI {poiEntity.Name} with id {pointOfInterestId} has been deleted.");

            return NoContent();

            // Old code:

            //var city = _citiesDataStore.Cities.First(c => c.Id == cityId);
            //if (city == null)
            //{
            //    return NotFound();
            //}

            //var pointOfInterestFromStore = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);
            //if (pointOfInterestFromStore == null)
            //{
            //    return NotFound();
            //}

            //city.PointsOfInterest.Remove(pointOfInterestFromStore);

            //_mailService.Send("PoI deleted.", $"PoI {pointOfInterestFromStore.Name} with id {pointOfInterestId} has been deleted.");
            //return NoContent();
        }
    }
}
