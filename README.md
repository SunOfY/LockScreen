# LockScreen

Tiện ích riêng tư dành cho Windows 10/11 với giao diện tiếng Việt. LockScreen phủ đen các màn hình trong phiên làm việc hiện tại. Ứng dụng yêu cầu Windows giữ máy thức, nhưng việc thu dữ liệu liên tục còn phụ thuộc ứng dụng và thiết bị USB.

## Sử dụng

Mở LockScreen.exe, đặt mật khẩu và bấm **Bật chế độ riêng tư**. Khi LockScreen đang chạy, dùng **Ctrl + Alt + K** để kích hoạt. Chạm phím hoặc nhấp chuột để hiện ô mật khẩu. Windows Hello khả dụng trên các máy có hỗ trợ.

Trong **Cài đặt ảnh và email**, chọn nơi lưu ảnh và các hành động sau khi nhập sai mật khẩu. Gmail hỗ trợ mật khẩu ứng dụng hoặc OAuth Desktop. Chế độ SMTP tùy chỉnh yêu cầu máy chủ hỗ trợ STARTTLS. Với OAuth, bật Gmail API cho dự án của bạn và nhập tệp cấu hình Desktop client. Dùng chức năng gửi thư thử để kiểm tra Gmail.

## Biên dịch

Trên Windows x64, cài .NET Framework 4.8 Developer Pack và Windows 10 hoặc 11 SDK. Chạy trong PowerShell ở thư mục dự án:

~~~powershell
.\build.ps1
~~~

Chạy bin\LockScreen.exe. Script dùng trình biên dịch C# của .NET Framework và tự tìm phiên bản Windows SDK. Mã nguồn nằm trong src. Shield.cs xử lý cửa sổ và phiên riêng tư, Style.cs định nghĩa giao diện, Options.cs quản lý cài đặt và SMTP, Gmail.cs xử lý Gmail OAuth.

## Dữ liệu và giới hạn

LockScreen tiếp tục dùng thư mục cấu hình cũ dưới %LOCALAPPDATA%\ManChe để giữ tương thích. Mật khẩu mở khóa được băm với PBKDF2 và salt ngẫu nhiên. Cấu hình email được Windows DPAPI bảo vệ theo tài khoản Windows. Ảnh JPEG lưu trong thư mục đã chọn. Không đưa cấu hình cá nhân, ảnh hoặc khóa OAuth vào kho mã nguồn.

Người có quyền truy cập phiên Windows vẫn có thể dùng Task Manager để kết thúc LockScreen. Phần mềm không thay thế cơ chế xác thực và khóa phiên của hệ điều hành.

## Phím tắt và chạy nền

Chọn **Phím tắt và khởi động** để thay Ctrl + Alt + K. Có thể phối hợp Ctrl, Alt hoặc Shift với một chữ cái, số hoặc F1–F11. Khi tổ hợp đang được dùng, LockScreen giữ thiết lập trước đó.

Đóng hoặc thu nhỏ cửa sổ để giữ LockScreen trong khay hệ thống. Nhấp đúp biểu tượng để mở, hoặc dùng menu chuột phải để thoát. Bật **Chạy ngầm khi đăng nhập Windows** để tự khởi động theo tài khoản đang dùng. Đặt file EXE ở vị trí cố định trước khi bật tùy chọn. Nếu di chuyển file, mở lại từ vị trí mới và lưu lại lựa chọn khởi động.

English is the default interface language. Open the Language menu to select English, Tiếng Việt or 繁體中文. Your selection is saved for the next launch.
Lock apps hides top-level windows of selected running processes. Unlock with your password or Windows Hello. New instances and direct file access are outside this protection. After an unexpected exit, reopen LockScreen to restore hidden windows.
The app picker groups running windows by executable path and displays the executable icon. Save selection remembers apps for Lock now and its global shortcut, including across restarts. Save and lock also hides running matches immediately. Use Unlock apps to restore protected windows.
A detected attempt to show a guarded window opens the unlock prompt. Window events arrive after Windows starts showing the window, so a brief flash is possible. Successful authentication restores all apps in the protected group.
