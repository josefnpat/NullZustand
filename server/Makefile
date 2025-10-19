# Project settings
PROJECT_NAME = NullZustand
SRC_DIR = src
BUILD_DIR = build
EXE = $(BUILD_DIR)/$(PROJECT_NAME).exe
PORT = 8140

# Compiler
MCS = mcs

# Source files
SRC = $(wildcard $(SRC_DIR)/*.cs) \
      $(wildcard $(SRC_DIR)/MessageHandlers/*.cs) \
      $(wildcard $(SRC_DIR)/MessageHandlers/Handlers/*.cs)
UNITY_SRC = ../client/Assets/Scripts/Shared/Messages.cs

# Default target
all: prepare $(EXE)

# Prepare build directory
prepare:
	mkdir -p $(BUILD_DIR)

# Compile using Mono C# compiler
$(EXE): $(SRC) $(UNITY_SRC)
	@echo "Compiling C# sources..."
	$(MCS) \
		-sdk:4.5 \
		-langversion:latest \
		-out:$@ \
		-target:exe \
		-r:System.Net.dll \
		-r:System.Core.dll \
		-r:packages/Newtonsoft.Json.13.0.4/lib/net45/Newtonsoft.Json.dll \
		$(SRC) $(UNITY_SRC)
	cp packages/Newtonsoft.Json.13.0.4/lib/net45/Newtonsoft.Json.dll build/
	@echo "Build complete → $(EXE)"

# Run the server
run: all
	@echo "Starting server with Mono on port $(PORT)..."
	mono $(EXE) --port $(PORT)

# Clean build artifacts
clean:
	rm -rf $(BUILD_DIR)

.PHONY: all clean run deps prepare

