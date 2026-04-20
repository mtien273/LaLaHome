using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using TRo123.Models;
using TRo123.Services;

namespace TRo123.Controllers
{
    public class HomeController(ILaLaHomeRepository repository) : Controller
    {
        private const string SessionMaTaiKhoan = "MaTaiKhoan";
        private const string SessionVaiTro = "VaiTro";

        private string? CurrentUserId => HttpContext.Session.GetString(SessionMaTaiKhoan);
        private string? CurrentRole => HttpContext.Session.GetString(SessionVaiTro);

        public async Task<IActionResult> Index()
        {
            var dsPhong = await repository.LayDanhSachPhongAsync(6);
            return View("trang_chu", dsPhong);
        }

        public async Task<IActionResult> Listings()
        {
            var dsPhong = await repository.LayDanhSachPhongAsync();
            return View("danh_sach_tin", dsPhong);
        }

        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var dsPhong = await repository.LayDanhSachPhongAsync(1);
                id = dsPhong.FirstOrDefault()?.MaPhong ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Chưa có dữ liệu phòng trong database.";
                return RedirectToAction(nameof(Listings));
            }

            var chiTiet = await repository.LayChiTietPhongChoPhepAsync(
                id,
                CurrentUserId,
                string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase));
            if (chiTiet is null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng cần xem.";
                return RedirectToAction(nameof(Listings));
            }

            return View("chi_tiet_phong", chiTiet);
        }

        public IActionResult CreatePost()
        {
            return RedirectToAction(nameof(CreatePostForm));
        }

        public async Task<IActionResult> CreatePostForm()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đăng tin.";
                return RedirectToAction(nameof(Login));
            }

            await LoadDanhMucAsync();
            var user = await repository.LayTaiKhoanTheoMaAsync(CurrentUserId);
            var model = new TaoTinPhongViewModel
            {
                MaTaiKhoan = CurrentUserId,
                SoDienThoai = user?.SoDienThoai ?? string.Empty
            };
            return View("dang_tin_moi", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(TaoTinPhongViewModel model, IFormFile? anhPhong, [FromServices] IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đăng tin.";
                return RedirectToAction(nameof(Login));
            }

            model.MaTaiKhoan = CurrentUserId;
            if (!ModelState.IsValid)
            {
                await LoadDanhMucAsync(model.MaTinhThanhPho, model.MaQuanHuyen);
                return View("dang_tin_moi", model);
            }

            var maPhongMoi = await repository.TaoTinPhongAsync(model);
            if (anhPhong != null && anhPhong.Length > 0)
            {
                var duongDan = await repository.UploadAnhAsync(anhPhong, env);
                await repository.LuuAnhVaoDbAsync(maPhongMoi, model.MaTaiKhoan, duongDan);
            }

            TempData["SuccessMessage"] = $"Đăng tin thành công. Mã phòng: {maPhongMoi} (chờ duyệt)";
            return RedirectToAction(nameof(Detail), new { id = maPhongMoi });
        }

        public IActionResult Login()
        {
            return View("dang_nhap", new DangNhapViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(DangNhapViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("dang_nhap", model);
            }

            var taiKhoan = await repository.LayTaiKhoanTheoDangNhapAsync(model);
            if (taiKhoan is null)
            {
                ModelState.AddModelError(string.Empty, "Sai số điện thoại hoặc mật khẩu.");
                return View("dang_nhap", model);
            }

            HttpContext.Session.SetString(SessionMaTaiKhoan, taiKhoan.MaTaiKhoan);
            HttpContext.Session.SetString(SessionVaiTro, taiKhoan.VaiTro);
            TempData["SuccessMessage"] = "Đăng nhập thành công.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Register()
        {
            return View("dang_ky", new DangKyTaiKhoanViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DangKyTaiKhoanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("dang_ky", model);
            }

            try
            {
                var maTaiKhoan = await repository.TaoTaiKhoanAsync(model);
                TempData["SuccessMessage"] = $"Đăng ký thành công. Mã tài khoản: {maTaiKhoan}";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Không thể tạo tài khoản: {ex.Message}");
                return View("dang_ky", model);
            }
        }

        public IActionResult Profile()
        {
            return RedirectToAction(nameof(ProfileForm));
        }

        public async Task<IActionResult> ProfileForm()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin cá nhân.";
                return RedirectToAction(nameof(Login));
            }

            var user = await repository.LayTaiKhoanTheoMaAsync(CurrentUserId);
            if (user is null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }

            var model = new CapNhatTaiKhoanViewModel
            {
                MaTaiKhoan = user.MaTaiKhoan,
                SoDienThoai = user.SoDienThoai,
                HoTen = user.HoTen
            };
            return View("thong_tin_ca_nhan", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CapNhatTaiKhoanViewModel model)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để cập nhật thông tin.";
                return RedirectToAction(nameof(Login));
            }

            model.MaTaiKhoan = CurrentUserId;
            if (!ModelState.IsValid)
            {
                return View("thong_tin_ca_nhan", model);
            }

            await repository.CapNhatThongTinTaiKhoanAsync(model);
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(ProfileForm));
        }

        public IActionResult EditPost(string id = "P0001")
        {
            return RedirectToAction(nameof(EditPostForm), new { id });
        }

        public async Task<IActionResult> EditPostForm(string id)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để chỉnh sửa tin.";
                return RedirectToAction(nameof(Login));
            }

            var model = await repository.LayTinPhongDeSuaAsync(id);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin cần chỉnh sửa.";
                return RedirectToAction(nameof(Listings));
            }

            if (!string.Equals(model.MaTaiKhoan, CurrentUserId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tin này.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            await LoadDanhMucAsync(model.MaTinhThanhPho, model.MaQuanHuyen);
            ViewData["PostId"] = id;
            return View("chinh_sua_tin_dang", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(TaoTinPhongViewModel model)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để chỉnh sửa tin.";
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                await LoadDanhMucAsync(model.MaTinhThanhPho, model.MaQuanHuyen);
                ViewData["PostId"] = model.MaPhong ?? string.Empty;
                return View("chinh_sua_tin_dang", model);
            }

            var current = await repository.LayTinPhongDeSuaAsync(model.MaPhong ?? string.Empty);
            if (current is null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin cần chỉnh sửa.";
                return RedirectToAction(nameof(Listings));
            }

            if (!string.Equals(current.MaTaiKhoan, CurrentUserId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tin này.";
                return RedirectToAction(nameof(Detail), new { id = model.MaPhong });
            }

            model.MaTaiKhoan = current.MaTaiKhoan;
            await repository.CapNhatTinPhongAsync(model);
            TempData["SuccessMessage"] = "Cập nhật tin thành công.";
            return RedirectToAction(nameof(Detail), new { id = model.MaPhong });
        }

        public IActionResult ReportPost(string id = "P0001")
        {
            ViewData["PostId"] = id;
            return View("bao_cao_bai_viet");
        }

        public IActionResult AccountManagement()
        {
            return RedirectToAction(nameof(AccountManagementList));
        }

        public async Task<IActionResult> AccountManagementList()
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            var ds = await repository.LayDanhSachTaiKhoanAsync();
            return View("quan_ly_tai_khoan_nguoi_dung", ds);
        }

        public async Task<IActionResult> AccountEdit(string id)
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            var user = await repository.LayTaiKhoanTheoMaAsync(id);
            if (user is null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(AccountManagementList));
            }

            var model = new CapNhatTaiKhoanQuanTriViewModel
            {
                MaTaiKhoan = user.MaTaiKhoan,
                HoTen = user.HoTen,
                SoDienThoai = user.SoDienThoai,
                VaiTro = user.VaiTro
            };
            return View("cap_nhat_tai_khoan_nguoi_dung", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccountEdit(CapNhatTaiKhoanQuanTriViewModel model)
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View("cap_nhat_tai_khoan_nguoi_dung", model);
            }

            await repository.CapNhatTaiKhoanBoiQuanTriAsync(model);
            TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction(nameof(AccountEdit), new { id = model.MaTaiKhoan });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string maTaiKhoan)
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            // Mặc định cấp lại mật khẩu: 123456 (đúng constraint 6-12)
            await repository.CapLaiMatKhauAsync(maTaiKhoan, "123456");
            TempData["SuccessMessage"] = $"Đã cấp lại mật khẩu cho {maTaiKhoan} (mật khẩu mới: 123456).";
            return RedirectToAction(nameof(AccountEdit), new { id = maTaiKhoan });
        }

        public async Task<IActionResult> Moderation()
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            var ds = await repository.LayDanhSachTinChoDuyetAsync();
            return View("kiem_duyet_tin", ds);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePost(string maPhong)
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            await repository.CapNhatTrangThaiDuyetTinAsync(maPhong, "KD001", true);
            TempData["SuccessMessage"] = $"Đã duyệt tin {maPhong}.";
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPost(string maPhong)
        {
            if (!string.Equals(CurrentRole, "QuanTri", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction(nameof(Index));
            }

            await repository.CapNhatTrangThaiDuyetTinAsync(maPhong, "KD003", false);
            TempData["SuccessMessage"] = $"Đã từ chối tin {maPhong}.";
            return RedirectToAction(nameof(Moderation));
        }

        [HttpGet]
        public async Task<IActionResult> QuanHuyen(string maTinh)
        {
            var ds = await repository.LayQuanHuyenAsync(maTinh);
            return Json(ds);
        }

        [HttpGet]
        public async Task<IActionResult> XaPhuong(string maQuan)
        {
            var ds = await repository.LayXaPhuongAsync(maQuan);
            return Json(ds);
        }

        private async Task LoadDanhMucAsync(string? maTinh = null, string? maQuan = null)
        {
            ViewData["LoaiPhong"] = await repository.LayLoaiPhongAsync();
            ViewData["TinhThanhPho"] = await repository.LayTinhThanhPhoAsync();
            ViewData["QuanHuyen"] = string.IsNullOrWhiteSpace(maTinh) ? [] : await repository.LayQuanHuyenAsync(maTinh);
            ViewData["XaPhuong"] = string.IsNullOrWhiteSpace(maQuan) ? [] : await repository.LayXaPhuongAsync(maQuan);
        }

        public IActionResult Privacy()
        {
            return View("chinh_sach_bao_mat");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
