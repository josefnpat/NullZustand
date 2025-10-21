# Project settings
PROJECT_NAME = NullZustand-Client
BUILD_DIR = Builds
PROJECT_PATH = $(shell pwd)

# Unity settings
UNITY_VERSION = 6000.2.8f1

# Build options
ARCHIVE ?= 1

# Detect host OS
UNAME_S := $(shell uname -s)
ifeq ($(OS),Windows_NT)
	HOST_OS := Windows
else ifeq ($(UNAME_S),Linux)
	HOST_OS := Linux
else ifeq ($(UNAME_S),Darwin)
	HOST_OS := macOS
else
	HOST_OS := Unknown
endif

# Unity executable path (based on host OS)
ifeq ($(HOST_OS),macOS)
	UNITY_APP = /Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/MacOS/Unity
else ifeq ($(HOST_OS),Linux)
	UNITY_APP = ~/Unity/Hub/Editor/$(UNITY_VERSION)/Editor/Unity
else ifeq ($(HOST_OS),Windows)
	UNITY_APP = C:/Program Files/Unity/Hub/Editor/$(UNITY_VERSION)/Editor/Unity.exe
else
	$(error Unsupported host operating system: $(HOST_OS))
endif

# Platform-specific build settings
MACOS_TARGET = StandaloneOSX
MACOS_METHOD = -buildOSXUniversalPlayer
MACOS_OUTPUT = $(BUILD_DIR)/macOS/$(PROJECT_NAME).app

LINUX_TARGET = StandaloneLinux64
LINUX_METHOD = -buildLinux64Player
LINUX_OUTPUT = $(BUILD_DIR)/linux/$(PROJECT_NAME)

WINDOWS_TARGET = StandaloneWindows64
WINDOWS_METHOD = -buildWindows64Player
WINDOWS_OUTPUT = $(BUILD_DIR)/windows/$(PROJECT_NAME).exe

# Default target - build for current OS
.DEFAULT_GOAL := current

# Build for all platforms
all: macos linux windows
	@echo "[BUILD] All platform builds complete!"

# Check if Unity is installed
check-unity:
	@echo "[UNITY] Host OS: $(HOST_OS)"
	@if [ ! -f "$(UNITY_APP)" ]; then \
		echo "[ERROR] Unity $(UNITY_VERSION) not found at $(UNITY_APP)"; \
		echo "[ERROR] Please install Unity $(UNITY_VERSION) or update UNITY_APP in Makefile"; \
		exit 1; \
	fi
	@echo "[UNITY] Found Unity $(UNITY_VERSION)"

# Build for current/host OS
current: check-unity
ifeq ($(HOST_OS),macOS)
	@$(MAKE) macos
else ifeq ($(HOST_OS),Linux)
	@$(MAKE) linux
else ifeq ($(HOST_OS),Windows)
	@$(MAKE) windows
else
	@echo "[ERROR] Cannot build for unknown OS: $(HOST_OS)"
	@exit 1
endif

# Internal function to build for a specific platform
# Usage: $(call build-platform,PLATFORM_NAME,BUILD_TARGET,BUILD_METHOD,OUTPUT_PATH,PLATFORM_DIR,ZIP_EXCLUDES)
define build-platform
	@echo "[BUILD] Building for $(1)..."
	@mkdir -p $(dir $(4))
	@"$(UNITY_APP)" \
		-quit \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-buildTarget $(2) \
		$(3) "$(PROJECT_PATH)/$(4)" \
		-logFile "$(PROJECT_PATH)/$(BUILD_DIR)/build-$(5).log"
	@if [ -e "$(4)" ]; then \
		echo "[BUILD] $(1) build complete → $(4)"; \
		if [ "$(ARCHIVE)" = "1" ]; then \
			echo "[ZIP] Creating $(1) archive..."; \
			cd $(BUILD_DIR)/$(5) && zip -r -q -X "../../$(BUILD_DIR)/$(PROJECT_NAME)_$(5).zip" . $(6); \
			echo "[ZIP] Archive created → $(BUILD_DIR)/$(PROJECT_NAME)_$(5).zip"; \
		fi; \
	else \
		echo "[ERROR] $(1) build failed. Check log: $(BUILD_DIR)/build-$(5).log"; \
		exit 1; \
	fi
endef

# Build for macOS
macos: check-unity
	$(call build-platform,macOS,$(MACOS_TARGET),$(MACOS_METHOD),$(MACOS_OUTPUT),macOS,-x "*.DS_Store" -x "__MACOSX/*" -x "*/._*")

# Build for Linux
linux: check-unity
	$(call build-platform,Linux,$(LINUX_TARGET),$(LINUX_METHOD),$(LINUX_OUTPUT),linux,-x "*.DS_Store" -x "__MACOSX/*" -x "*/._*" -x "*~")

# Build for Windows
windows: check-unity
	$(call build-platform,Windows,$(WINDOWS_TARGET),$(WINDOWS_METHOD),$(WINDOWS_OUTPUT),windows,-x "*.DS_Store" -x "__MACOSX/*" -x "*/._*" -x "Thumbs.db" -x "desktop.ini")

# Run the application for current OS
run:
ifeq ($(HOST_OS),macOS)
	@if [ ! -e "$(MACOS_OUTPUT)" ]; then \
		echo "[ERROR] macOS build not found. Run 'make macos' first."; \
		exit 1; \
	fi
	@echo "[RUN] Launching macOS build..."
	@open "$(MACOS_OUTPUT)"
else ifeq ($(HOST_OS),Linux)
	@if [ ! -e "$(LINUX_OUTPUT)" ]; then \
		echo "[ERROR] Linux build not found. Run 'make linux' first."; \
		exit 1; \
	fi
	@echo "[RUN] Launching Linux build..."
	@"$(LINUX_OUTPUT)"
else ifeq ($(HOST_OS),Windows)
	@if [ ! -e "$(WINDOWS_OUTPUT)" ]; then \
		echo "[ERROR] Windows build not found. Run 'make windows' first."; \
		exit 1; \
	fi
	@echo "[RUN] Launching Windows build..."
	@"$(WINDOWS_OUTPUT)"
endif

# Clean build artifacts
clean:
	@echo "[CLEAN] Removing build artifacts..."
	@rm -rf $(BUILD_DIR)
	@rm -rf Library/Artifacts
	@rm -rf Library/BuildPlayer.prefs
	@rm -rf Library/BuildPlayerData
	@rm -rf Temp
	@echo "[CLEAN] Clean complete"

# Clean only the build output (keep Unity cache)
clean-build:
	@echo "[CLEAN] Removing build output..."
	@rm -rf $(BUILD_DIR)
	@echo "[CLEAN] Build output removed"

# Show build logs
log:
	@echo "=== macOS Build Log ==="
	@if [ -f "$(BUILD_DIR)/build-macos.log" ]; then \
		cat "$(BUILD_DIR)/build-macos.log"; \
	else \
		echo "[INFO] macOS log not found"; \
	fi
	@echo ""
	@echo "=== Linux Build Log ==="
	@if [ -f "$(BUILD_DIR)/build-linux.log" ]; then \
		cat "$(BUILD_DIR)/build-linux.log"; \
	else \
		echo "[INFO] Linux log not found"; \
	fi
	@echo ""
	@echo "=== Windows Build Log ==="
	@if [ -f "$(BUILD_DIR)/build-windows.log" ]; then \
		cat "$(BUILD_DIR)/build-windows.log"; \
	else \
		echo "[INFO] Windows log not found"; \
	fi

# Show specific platform log
log-macos:
	@if [ -f "$(BUILD_DIR)/build-macos.log" ]; then \
		cat "$(BUILD_DIR)/build-macos.log"; \
	else \
		echo "[ERROR] macOS log not found: $(BUILD_DIR)/build-macos.log"; \
	fi

log-linux:
	@if [ -f "$(BUILD_DIR)/build-linux.log" ]; then \
		cat "$(BUILD_DIR)/build-linux.log"; \
	else \
		echo "[ERROR] Linux log not found: $(BUILD_DIR)/build-linux.log"; \
	fi

log-windows:
	@if [ -f "$(BUILD_DIR)/build-windows.log" ]; then \
		cat "$(BUILD_DIR)/build-windows.log"; \
	else \
		echo "[ERROR] Windows log not found: $(BUILD_DIR)/build-windows.log"; \
	fi

# Show Unity installation info
info:
	@echo "Host OS:          $(HOST_OS)"
	@echo "Project Name:     $(PROJECT_NAME)"
	@echo "Project Path:     $(PROJECT_PATH)"
	@echo "Unity Version:    $(UNITY_VERSION)"
	@echo "Unity Executable: $(UNITY_APP)"
	@echo "Archive:          $(ARCHIVE) (1=enabled, 0=disabled)"
	@echo ""
	@echo "Build Outputs:"
	@echo "  macOS:   $(MACOS_OUTPUT)"
	@echo "  Linux:   $(LINUX_OUTPUT)"
	@echo "  Windows: $(WINDOWS_OUTPUT)"
	@echo ""
	@echo "Archives (when ARCHIVE=1):"
	@echo "  macOS:   $(BUILD_DIR)/$(PROJECT_NAME)_macOS.zip"
	@echo "  Linux:   $(BUILD_DIR)/$(PROJECT_NAME)_linux.zip"
	@echo "  Windows: $(BUILD_DIR)/$(PROJECT_NAME)_windows.zip"

# Show help
help:
	@echo "Unity Headless Build Makefile"
	@echo ""
	@echo "Targets:"
	@echo "  make              - Build for current OS (same as 'make current')"
	@echo "  make current      - Build for current OS"
	@echo "  make macos        - Build for macOS"
	@echo "  make linux        - Build for Linux"
	@echo "  make windows      - Build for Windows"
	@echo "  make all          - Build for all platforms"
	@echo "  make run          - Run the built application for current OS"
	@echo "  make clean        - Remove all build artifacts"
	@echo "  make clean-build  - Remove only build output (keep Unity cache)"
	@echo "  make log          - Show all build logs"
	@echo "  make log-macos    - Show macOS build log"
	@echo "  make log-linux    - Show Linux build log"
	@echo "  make log-windows  - Show Windows build log"
	@echo "  make info         - Show project and Unity info"
	@echo "  make help         - Show this help message"
	@echo ""
	@echo "Options:"
	@echo "  ARCHIVE=1         - Create zip archives (default, enabled)"
	@echo "  ARCHIVE=0         - Skip zip archives"
	@echo ""
	@echo "Examples:"
	@echo "  make              - Build for current OS with archiving"
	@echo "  make ARCHIVE=0    - Build for current OS without archiving"
	@echo "  make all ARCHIVE=1 - Build all platforms with archiving"

.PHONY: all current macos linux windows run clean clean-build check-unity log log-macos log-linux log-windows info help

