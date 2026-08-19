#include <flutter/method_channel.h>
#include <flutter/standard_method_codec.h>
#include "flutter_window.h"

#include <windows.h>
#include <fstream>
#include <sstream>
#include <optional>
#include <variant>

#include "flutter/generated_plugin_registrant.h"

namespace {
constexpr wchar_t kPipeName[] = L"\\\\.\\pipe\\PowerProfile.LocalApi.v1";

std::string RequestPipe(const std::string& request) {
  HANDLE pipe = CreateFileW(kPipeName, GENERIC_READ | GENERIC_WRITE, 0, nullptr,
                            OPEN_EXISTING, 0, nullptr);
  if (pipe == INVALID_HANDLE_VALUE) {
    return {};
  }

  DWORD written = 0;
  const std::string framed = request + "\n";
  const bool write_ok = WriteFile(pipe, framed.data(), static_cast<DWORD>(framed.size()), &written, nullptr);
  std::string response;
  if (write_ok) {
    char buffer[16 * 1024];
    DWORD read = 0;
    while (ReadFile(pipe, buffer, sizeof(buffer), &read, nullptr) && read > 0) {
      response.append(buffer, read);
      if (response.find('\n') != std::string::npos) break;
    }
  }
  CloseHandle(pipe);
  const auto newline = response.find('\n');
  if (newline != std::string::npos) response.resize(newline);
  return response;
}
}  // namespace

FlutterWindow::FlutterWindow(const flutter::DartProject& project)
    : project_(project) {}

FlutterWindow::~FlutterWindow() {}

bool FlutterWindow::OnCreate() {
  if (!Win32Window::OnCreate()) {
    return false;
  }

  RECT frame = GetClientArea();

  // The size here must match the window dimensions to avoid unnecessary surface
  // creation / destruction in the startup path.
  flutter_controller_ = std::make_unique<flutter::FlutterViewController>(
      frame.right - frame.left, frame.bottom - frame.top, project_);
  // Ensure that basic setup of the controller was successful.
  if (!flutter_controller_->engine() || !flutter_controller_->view()) {
    return false;
  }
  RegisterPlugins(flutter_controller_->engine());
  const flutter::MethodChannel<> channel(
      flutter_controller_->engine()->messenger(), "powerprofile/local_pipe",
      &flutter::StandardMethodCodec::GetInstance());
  channel.SetMethodCallHandler(
      [](const auto& call, auto result) {
        if (call.method_name() != "request" || !call.arguments() ||
            !std::holds_alternative<std::string>(*call.arguments())) {
          result->Error("invalid_method", "Expected a request string.");
          return;
        }
        const auto response = RequestPipe(std::get<std::string>(*call.arguments()));
        if (response.empty()) {
          result->Error("pipe_unavailable", "PowerProfile native host pipe is unavailable.");
          return;
        }
        result->Success(flutter::EncodableValue(response));
      });
  SetChildContent(flutter_controller_->view()->GetNativeWindow());

  flutter_controller_->engine()->SetNextFrameCallback([&]() {
    this->Show();
  });

  // Flutter can complete the first frame before the "show window" callback is
  // registered. The following call ensures a frame is pending to ensure the
  // window is shown. It is a no-op if the first frame hasn't completed yet.
  flutter_controller_->ForceRedraw();

  return true;
}

void FlutterWindow::OnDestroy() {
  if (flutter_controller_) {
    flutter_controller_ = nullptr;
  }

  Win32Window::OnDestroy();
}

LRESULT
FlutterWindow::MessageHandler(HWND hwnd, UINT const message,
                              WPARAM const wparam,
                              LPARAM const lparam) noexcept {
  // Give Flutter, including plugins, an opportunity to handle window messages.
  if (flutter_controller_) {
    std::optional<LRESULT> result =
        flutter_controller_->HandleTopLevelWindowProc(hwnd, message, wparam,
                                                      lparam);
    if (result) {
      return *result;
    }
  }

  switch (message) {
    case WM_FONTCHANGE:
      flutter_controller_->engine()->ReloadSystemFonts();
      break;
  }

  return Win32Window::MessageHandler(hwnd, message, wparam, lparam);
}
