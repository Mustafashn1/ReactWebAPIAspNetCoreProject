using Microsoft.AspNetCore.Mvc;
using PizzaStore.Data;
using PizzaStore.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context; // IDiagnosticContext için gerekli using

namespace PizzaStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PizzaController : ControllerBase
    {
        private readonly AppDbContext _context;
        readonly IDiagnosticContext _diagnosticContext; // IDiagnosticContext'i tanımlayın

        public PizzaController(AppDbContext context, IDiagnosticContext diagnosticContext) // Yapıcı metodunu güncelle
        {
            _context = context;
            _diagnosticContext = diagnosticContext ?? 
                throw new ArgumentNullException(nameof(diagnosticContext)); // Null kontrolü ekleyin
        }
        

        // GET: api/pizza
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pizza>>> GetPizzas()
        {
            Log.Information("Tüm pizzalar alınıyor.");
            var pizzas = await _context.Pizzas.ToListAsync();
            Log.Information("{Count} pizza bulundu.", pizzas.Count);
            return pizzas;
        }

        // GET: api/pizza/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Pizza>> GetPizza(int id)
        {
            Log.Information("{Id} numaralı pizza alınıyor.", id);
            var pizza = await _context.Pizzas.FindAsync(id);

            if (pizza == null)
            {
                Log.Warning("{Id} numaralı pizza bulunamadı.", id);
                return NotFound();
            }

            Log.Information("{Id} numaralı pizza bulundu.", id);
            return pizza;
        }

        // POST: api/pizza
        [HttpPost]
        public async Task<ActionResult<Pizza>> PostPizza(Pizza pizza)
        {
            Log.Information("Yeni pizza ekleniyor: {@Pizza}", pizza);
            _context.Pizzas.Add(pizza);
            await _context.SaveChangesAsync();

            Log.Information("{Id} numaralı pizza başarıyla eklendi.", pizza.Id);
            return CreatedAtAction(nameof(GetPizza), new { id = pizza.Id }, pizza);
        }

        // PUT: api/pizza/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPizza(int id, Pizza pizza)
        {
            if (id != pizza.Id)
            {
                Log.Warning("Pizza güncellenemedi. Geçersiz ID: {Id}", id);
                return BadRequest();
            }

            Log.Information("{Id} numaralı pizza güncelleniyor.", id);
            _context.Entry(pizza).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("{Id} numaralı pizza başarıyla güncellendi.", id);
            }
            catch (DbUpdateConcurrencyException ex) // 'ex' değişkeni burada tanımlandı
            {
                if (!PizzaExists(id))
                {
                    Log.Warning("{Id} numaralı pizza bulunamadı.", id);
                    return NotFound();
                }
                else
                {
                    Log.Error("Güncelleme sırasında bir hata oluştu: {Message}", ex.Message);
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/pizza/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePizza(int id)
        {
            Log.Information("{Id} numaralı pizza siliniyor.", id);
            var pizza = await _context.Pizzas.FindAsync(id);
            if (pizza == null)
            {
                Log.Warning("{Id} numaralı pizza bulunamadı, silme işlemi yapılamadı.", id);
                return NotFound();
            }

            _context.Pizzas.Remove(pizza);
            await _context.SaveChangesAsync();

            Log.Information("{Id} numaralı pizza başarıyla silindi.", id);
            return Ok(pizza);
        }

       

        private bool PizzaExists(int id)
        {
            return _context.Pizzas.Any(e => e.Id == id);
        }
    }
}
