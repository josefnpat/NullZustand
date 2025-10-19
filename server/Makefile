# Project settings
PROJECT_NAME = NullZustand
SRC_DIR = src
BUILD_DIR = build
EXE = $(BUILD_DIR)/$(PROJECT_NAME).exe
PORT = 8140

# Certificate paths
SERVER_CERT = $(BUILD_DIR)/server.pfx
CLIENT_CERT = ../client/Assets/Scripts/Shared/server.cer
TEMP_KEY = $(BUILD_DIR)/temp_key.pem

# Compiler
MCS = mcs

# Source files
SRC = $(wildcard $(SRC_DIR)/*.cs) \
      $(wildcard $(SRC_DIR)/MessageHandlers/*.cs) \
      $(wildcard $(SRC_DIR)/MessageHandlers/Handlers/*.cs)
UNITY_SRC = ../client/Assets/Scripts/Shared/Messages.cs

# Default target
all: prepare certs $(EXE)

# Prepare build directory
prepare:
	mkdir -p $(BUILD_DIR)
	mkdir -p $(dir $(CLIENT_CERT))

# Generate SSL certificates if they don't exist
certs: $(SERVER_CERT) $(CLIENT_CERT)

$(SERVER_CERT) $(CLIENT_CERT):
	@if [ -f "$(SERVER_CERT)" ] && [ -f "$(CLIENT_CERT)" ]; then \
		echo "[CERT] Certificates already exist, skipping generation"; \
	else \
		echo "[CERT] Generating self-signed certificate (Mono-compatible)..."; \
		openssl req -x509 -newkey rsa:2048 -nodes \
			-keyout "$(TEMP_KEY)" \
			-out "$(CLIENT_CERT)" \
			-days 3650 \
			-subj "/CN=NullZustand Server" 2>/dev/null; \
		openssl pkcs12 -export -out "$(SERVER_CERT)" \
			-inkey "$(TEMP_KEY)" \
			-in "$(CLIENT_CERT)" \
			-legacy -passout pass: 2>/dev/null; \
		rm -f "$(TEMP_KEY)"; \
		echo "[CERT] Certificate generation complete"; \
		echo "[CERT] Private key: $(SERVER_CERT)"; \
		echo "[CERT] Public cert: $(CLIENT_CERT)"; \
	fi

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

# Clean build artifacts and certificates
clean:
	rm -rf $(BUILD_DIR)
	rm -f ../client/Assets/Scripts/Shared/server.cer

# Clean only certificates (useful for regenerating)
clean-certs:
	rm -f $(BUILD_DIR)/server.pfx
	rm -f ../client/Assets/Scripts/Shared/server.cer

.PHONY: all clean clean-certs run deps prepare certs

