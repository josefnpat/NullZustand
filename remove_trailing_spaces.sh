#!/bin/bash
find server/ client/Assets/Scripts/ -name "*.cs" -type f -exec sed -i '' 's/[[:space:]]*$//' {} \;
