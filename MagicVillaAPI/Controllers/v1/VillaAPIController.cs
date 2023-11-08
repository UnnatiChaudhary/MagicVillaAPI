using MagicVilla_VillaAPI.Models.Dto;
using MagicVillaAPI.Data;
using MagicVillaAPI.Logging;
using MagicVillaAPI.Models;
using MagicVillaAPI.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MagicVillaAPI.Controllers.v1
{
    [Route("api/v{version:apiVersion}/VillaAPI")]
    [ApiController]
    [ApiVersion("1.0", Deprecated =true)]

    public class VillaAPIController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICacheService _cacheService;

        private readonly ILogging _logger;
        public VillaAPIController(ApplicationDbContext db, ILogging logger, ICacheService cacheService)
        {
            _db = db;
            _logger = logger;
            _cacheService = cacheService;

        }

        [HttpGet("villas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVillas()
        {
            var cacheDrivers = _cacheService.GetData<IEnumerable<Villa>>("villas");
            if (cacheDrivers != null && cacheDrivers.Count() > 0)
            {
                return Ok(new { source = "cache", data = cacheDrivers ,version="1.0"});
            }
            var villas = await _db.Villas.ToListAsync();
            var expiryTime = DateTimeOffset.Now.AddMinutes(2);
            _cacheService.SetData<IEnumerable<Villa>>("villas", villas, expiryTime);

            return Ok(new { source = "database", data = villas,version="1.0" });
        }
       
        [HttpGet("{id:int}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetVilla(int id)
        {
            if (id == 0)
            {
                _logger.Log("Get Villa Error with Id" + id, "error");
                return BadRequest();
            }
            var cacheDrivers = _cacheService.GetData<IEnumerable<Villa>>("villas");
            var Villa = cacheDrivers?.FirstOrDefault(u => u.Id == id);
            if (Villa == null)
            {
                var villa = _db.Villas.FirstOrDefault(u => u.Id == id);
                if (villa == null)
                {
                    return NotFound();
                }
                var villas = _db.Villas.ToList();
                var expiryTime = DateTimeOffset.Now.AddMinutes(2);
                _cacheService.SetData<IEnumerable<Villa>>("villas", villas, expiryTime);
                return Ok(new { source = "database", data = villa, version = "1.0" });
            }
            else
            {
                return Ok(new { source = "cache", data = Villa, version = "1.0" });
            }
        }
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateVilla([FromBody] VillaDTO villaDTO)
        {

            if (villaDTO == null)
            {
                return BadRequest(villaDTO);
            }

            Villa model = new()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft,
            };
            _db.Villas.Add(model);
            _db.SaveChanges();
            _cacheService.RemoveData("villas");
            var villas = _db.Villas.ToList();
            var expiryTime = DateTimeOffset.Now.AddMinutes(2);
            _cacheService.SetData<IEnumerable<Villa>>("villas", villas, expiryTime);
            return CreatedAtRoute("GetVilla", new { id = model.Id }, model);
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        public IActionResult DeleteVilla(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var villa = _db.Villas.FirstOrDefault(u => u.Id == id);
            var AllVillas = _cacheService.GetData<IEnumerable<Villa>>("villas");

            if (AllVillas != null)
            {
                var RemainingVillas = AllVillas.Where(villa => villa.Id != id);
                if (RemainingVillas != null)
                {
                    var expiryTime = DateTimeOffset.Now.AddMinutes(2);
                    _cacheService.SetData("villas", RemainingVillas, expiryTime);
                }
            }
            if (villa == null)
            {
                return NotFound();
            }
            _db.Villas.Remove(villa);
            _db.SaveChanges();
            return NoContent();
        }
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPut("{id:int}", Name = "UpdateVilla")]
        public IActionResult UpdateVilla(int id, [FromBody] VillaDTO villaDTO)
        {
            if (villaDTO == null)
            {
                return BadRequest();
            }
            Villa model = new Villa()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft,
            };

            _db.Villas.Update(model);
            var allVillas = _cacheService.GetData<IEnumerable<Villa>>("villas");

            if (allVillas != null)
            {
                var remainingVillas = allVillas.Where(u => u.Id != id).ToList();
                remainingVillas.Add(model);

                remainingVillas = remainingVillas.OrderBy(student => student.Id).ToList();

                var expiryTime = DateTimeOffset.Now.AddMinutes(2);
                _cacheService.SetData<IEnumerable<Villa>>("villas", remainingVillas, expiryTime);
            }
            //_cacheService.RemoveData("villas");
            //var villas = _db.Villas.ToList();
            //var expiryTime = DateTimeOffset.Now.AddMinutes(2);
            //_cacheService.SetData<IEnumerable<Villa>>("villas", villas, expiryTime);
            _db.SaveChanges();
            return NoContent();
        }
        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaDTO> patchDTO)
        {
            if (patchDTO == null)
            {
                return BadRequest();
            }

            var villa = _db.Villas.AsNoTracking().FirstOrDefault(u => u.Id == id);

            VillaDTO villaDTO = new()
            {
                Amenity = villa.Amenity,
                Details = villa.Details,
                Id = villa.Id,
                ImageUrl = villa.ImageUrl,
                Name = villa.Name,
                Occupancy = villa.Occupancy,
                Rate = villa.Rate,
                Sqft = villa.Sqft,
            };
            if (villa == null)
            {
                return BadRequest();
            }
            patchDTO.ApplyTo(villaDTO, ModelState);
            Villa model = new()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft,
            };
            _db.Villas.Update(model);
            _db.SaveChanges();
            _cacheService.RemoveData("villas");
            var villas = _db.Villas.ToList();
            var expiryTime = DateTimeOffset.Now.AddMinutes(2);
            _cacheService.SetData<IEnumerable<Villa>>("villas", villas, expiryTime);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return NoContent();
        }
        //Checking health
        [HttpGet("/apihealth")]
        public IActionResult GetApiHealth()
        {
            return Ok(new {status="healthy" });
        }
    }
}

