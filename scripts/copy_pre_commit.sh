#!/bin/bash
# Copyright (c) Armidale Software
# SPDX-License-Identifier: MIT

FILE_PATH="$1"
DESTINATION=$(git rev-parse --git-path hooks)

echo "Copy $FILE_PATH to $DESTINATION."
cp "$FILE_PATH" "$DESTINATION"
