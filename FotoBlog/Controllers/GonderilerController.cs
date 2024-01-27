using FotoBlog.Data;
using FotoBlog.Models;
using Microsoft.AspNetCore.Mvc;

namespace FotoBlog.Controllers
{
    public class GonderilerController : Controller
    {
        private UygulamaDbContext _db;
        private IWebHostEnvironment _env;

        public GonderilerController(UygulamaDbContext db, IWebHostEnvironment env) 
        {
            _db = db;
            _env = env;
        }
        public IActionResult Yeni()
        {
            return View();
        }
        
        [HttpPost,ValidateAntiForgeryToken]        
        public IActionResult Yeni(YeniGonderiViewModel vm)
        {
            if (ModelState.IsValid)
            {
                string ext = Path.GetExtension(vm.Resim.FileName);
                string yeniDosyaAd = Guid.NewGuid() + ext;
                string yol = Path.Combine(_env.WebRootPath, "img","upload", yeniDosyaAd);

                using(var fs = new FileStream(yol, FileMode.CreateNew))
                {
                    vm.Resim.CopyTo(fs);
                }

                _db.Gonderiler.Add(new Gonderi
                {
                    Baslik = vm.Baslik,
                    ResimYolu = yeniDosyaAd
                });
                _db.SaveChanges();
                return RedirectToAction("Index", "Home", new { Sonuc = "Eklendi" });
            }

            return View();
        }
        public IActionResult Sil(int id)
        {
            Gonderi g = _db.Gonderiler.Find(id);
            return View(g);
        }
        [HttpPost]
        public IActionResult Sil(Gonderi gonderi)
        {
            ResmiKlasördenKaldır(gonderi);
            _db.Gonderiler.Remove(gonderi);
            _db.SaveChanges();
            return RedirectToAction("Index", "Home", new { Sonuc = "Silindi" });
        }

        public IActionResult Guncelle(int id)
        {
            Gonderi gg = _db.Gonderiler.Find(id);
            GuncellenecekGonderiViewModel gm = new()
            {
                Id = gg.Id,
                Baslik = gg.Baslik,
                ResimYolu = gg.ResimYolu
            };
            //TempData["ResimYolu"] = gg.ResimYolu;

            return View(gm);                
        }
        [HttpPost]
        public IActionResult Guncelle(GuncellenecekGonderiViewModel vm)
        {
            if (ModelState.IsValid)
            {
                Gonderi GuncellenecekGonderi = _db.Gonderiler.FirstOrDefault(g => g.Id == vm.Id)!;
                if (GuncellenecekGonderi != null)
                {
                    GuncellenecekGonderi.Id = vm.Id;
                    GuncellenecekGonderi.Baslik = vm.Baslik;

                    if (vm.ResimYolu != null)
                    {
                        GuncellenecekGonderi.ResimYolu = vm.ResimYolu;
                        if (Path.GetFileName(GuncellenecekGonderi.ResimYolu) != vm.Resim.FileName)
                        {
                            // Resim değişmişse ve eski resim başka bir gönderi tarafından kullanılmıyorsa kaldırız.
                            ResmiKlasördenKaldır(GuncellenecekGonderi);

                            // Yeni resmi ekle
                            string ext = Path.GetExtension(vm.Resim.FileName);
                            string yeniDosyaAdi = Guid.NewGuid() + ext;
                            string yol = Path.Combine(_env.WebRootPath, "img", "upload", yeniDosyaAdi);

                            using (var fs = new FileStream(yol, FileMode.CreateNew))
                            {
                                vm.Resim.CopyTo(fs);
                            }

                            GuncellenecekGonderi.ResimYolu = yeniDosyaAdi;
                        }
                    }
                    _db.SaveChanges();
                    return RedirectToAction("Index", "Home", new { Sonuc = "Duzenlendi" });
                }
                else
                {
                    return NotFound();
                }
            }

            return View(vm);
        }
        public void ResmiKlasördenKaldır(Gonderi gonderi)
        {
            if (gonderi.ResimYolu != null)
            {
                string silincekResimDosyaAdi = gonderi.ResimYolu;
                string dosyaYolu = Path.Combine(_env.WebRootPath, "img", "upload", silincekResimDosyaAdi);


                if (System.IO.File.Exists(dosyaYolu))
                {
                    bool baskabirGonderiTarafindanKullaniliyorMu = _db.Gonderiler.Any(g => g.ResimYolu == gonderi.ResimYolu && g.Id != gonderi.Id);

                    if (!baskabirGonderiTarafindanKullaniliyorMu)
                    {
                        System.IO.File.Delete(dosyaYolu);
                    }
                }
            }
        }
    }
}
