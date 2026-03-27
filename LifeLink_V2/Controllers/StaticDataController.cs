using LifeLink_V2.Data;
using LifeLink_V2.Helpers;
using LifeLink_V2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeLink_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaticDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StaticDataController> _logger;

        public StaticDataController(AppDbContext context, ILogger<StaticDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/staticdata/governorates
        [HttpGet("governorates")]
        public async Task<ActionResult<ApiResponse>> GetGovernorates()
        {
            try
            {
                var governorates = await _context.Governorates
                    .OrderBy(g => g.GovernorateName)
                    .Select(g => new
                    {
                        g.GovernorateId,
                        g.GovernorateName,
                        g.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب المحافظات بنجاح",
                    Data = governorates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching governorates");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب المحافظات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/cities/{governorateId}
        [HttpGet("cities/{governorateId}")]
        public async Task<ActionResult<ApiResponse>> GetCitiesByGovernorate(int governorateId)
        {
            try
            {
                var cities = await _context.Cities
                    .Where(c => c.GovernorateId == governorateId)
                    .OrderBy(c => c.CityName)
                    .Select(c => new
                    {
                        c.CityId,
                        c.GovernorateId,
                        c.CityName,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب المدن بنجاح",
                    Data = cities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cities for governorate {GovernorateId}", governorateId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب المدن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/insurance-companies
        [HttpGet("insurance-companies")]
        public async Task<ActionResult<ApiResponse>> GetInsuranceCompanies()
        {
            try
            {
                var companies = await _context.InsuranceCompanies
                    .Where(i => i.Active)
                    .OrderBy(i => i.CompanyName)
                    .Select(i => new
                    {
                        i.InsuranceCompanyId,
                        i.CompanyName,
                        i.Category,
                        i.Phone,
                        i.Email,
                        i.Address
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب شركات التأمين بنجاح",
                    Data = companies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching insurance companies");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب شركات التأمين",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/specialties
        [HttpGet("specialties")]
        public async Task<ActionResult<ApiResponse>> GetMedicalSpecialties()
        {
            try
            {
                var specialties = await _context.MedicalSpecialties
                    .OrderBy(s => s.SpecialtyName)
                    .Select(s => new
                    {
                        s.SpecialtyId,
                        s.SpecialtyName,
                        s.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب التخصصات الطبية بنجاح",
                    Data = specialties
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medical specialties");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب التخصصات الطبية",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/provider-types
        [HttpGet("provider-types")]
        public async Task<ActionResult<ApiResponse>> GetProviderTypes()
        {
            try
            {
                var providerTypes = await _context.ProviderTypes
                    .OrderBy(p => p.ProviderTypeName)
                    .Select(p => new
                    {
                        p.ProviderTypeId,
                        p.ProviderTypeName,
                        p.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب أنواع المؤسسات الطبية بنجاح",
                    Data = providerTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching provider types");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب أنواع المؤسسات الطبية",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/lab-test-categories
        [HttpGet("lab-test-categories")]
        public async Task<ActionResult<ApiResponse>> GetLabTestCategories()
        {
            try
            {
                var categories = await _context.LabTestCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب فئات التحاليل المخبرية بنجاح",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lab test categories");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب فئات التحاليل المخبرية",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/payment-methods
        [HttpGet("payment-methods")]
        public async Task<ActionResult<ApiResponse>> GetPaymentMethods()
        {
            try
            {
                var methods = await _context.PaymentMethods
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.MethodName)
                    .Select(p => new
                    {
                        p.PaymentMethodId,
                        p.MethodName,
                        p.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب طرق الدفع بنجاح",
                    Data = methods
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment methods");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب طرق الدفع",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/staticdata/appointment-statuses
        [HttpGet("appointment-statuses")]
        [Authorize(Roles = "Admin,Provider")]
        public async Task<ActionResult<ApiResponse>> GetAppointmentStatuses()
        {
            try
            {
                var statuses = await _context.AppointmentStatuses
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.StatusId)
                    .Select(s => new
                    {
                        s.StatusId,
                        s.StatusName,
                        s.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "تم جلب حالات المواعيد بنجاح",
                    Data = statuses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointment statuses");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء جلب حالات المواعيد",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

  
}