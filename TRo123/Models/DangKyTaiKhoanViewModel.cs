using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class DangKyTaiKhoanViewModel
{

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(30)]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đủ 10 số")]
    public string SoDienThoai { get; set; } = string.Empty;
    [RegularExpression(@"^0[3|5|7|8|9][0-9]{8}$", ErrorMessage = "SĐT không đúng định dạng")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(12, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 12 ký tự")]
    public string MatKhau { get; set; } = string.Empty;
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{6,12}$", ErrorMessage = "Mật khẩu phải có cả chữ và số")]
    [Required(ErrorMessage = "Vui lòng chọn loại tài khoản")]
    public string VaiTro { get; set; } = "NguoiDung"; // NguoiDung: người tìm kiếm, ChuTro: người cho thuê
}
