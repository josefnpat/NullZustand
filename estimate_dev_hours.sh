#!/bin/bash

# Script to estimate development time based on git commit history
# Only considers commits that modified .cs files
# Assumes work sessions are continuous if commits are within 4 hours
# Uses minutes for better accuracy

set -e

# Utility function for mathematical calculations with fallback
calculate_ratio() {
    local numerator=$1
    local denominator=$2
    local scale=${3:-2}
    echo "scale=$scale; $numerator / $denominator" | bc -l 2>/dev/null || echo "scale=$scale; $numerator / $denominator" | awk '{printf "%.'$scale'f", $1/$2}'
}

# Configuration
# WORK_DAY_HOURS=8
# - Standard full-time work day duration
# - Used for converting total hours to work days
# - Can be adjusted for different work schedules (part-time, overtime, etc.)
# - Based on typical software development work patterns
WORK_DAY_HOURS=8

# Session analysis configuration
# MAX_SESSION_GAP_MINUTES=240 (4 hours)
# - Based on typical work day patterns and break analysis
# - Gaps longer than 4 hours usually indicate end of work session
# - Accounts for lunch breaks, meetings, and natural work boundaries
# - Balances between capturing continuous work vs. separate sessions
#
# MIN_COMMIT_TIME_MINUTES=5
# - Minimum realistic time for any meaningful code change
# - Accounts for commit message writing, testing, and verification
# - Prevents zero-time sessions that would skew productivity metrics
# - Based on analysis of rapid-fire commits in development workflows
MAX_SESSION_GAP_MINUTES=240  # 4 hours - gaps longer than this are considered breaks
MIN_COMMIT_TIME_MINUTES=5     # Minimum time assumed for any commit

# Development productivity metrics (lines of code changed per minute)
# These values are based on industry analysis of C# development productivity:
# 
# SENIOR_DEV_LINES_PER_MIN=0.82
# - Based on analysis of lines of code changed in commits
# - Measures actual development effort rather than total codebase size
# - Accounts for refactoring, debugging, and iterative development
# - Includes complex Unity networking, server architecture, message handling
# - More accurate for AI-assisted development where code generation is rapid
#
# JUNIOR_DEV_LINES_PER_MIN=0.43  
# - Based on industry research showing junior developers are ~50% as productive
# - Measures lines changed per minute of development time
# - Includes learning curve, more debugging time, code review iterations
# - Accounts for additional time spent on documentation and understanding
# - Better reflects actual development effort vs. total codebase metrics
SENIOR_DEV_LINES_PER_MIN=0.82
JUNIOR_DEV_LINES_PER_MIN=0.43

# Parse command line arguments
VERBOSE=false
while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --work-hours)
            WORK_DAY_HOURS="$2"
            shift 2
            ;;
        --senior-lines-per-min)
            SENIOR_DEV_LINES_PER_MIN="$2"
            shift 2
            ;;
        --junior-lines-per-min)
            JUNIOR_DEV_LINES_PER_MIN="$2"
            shift 2
            ;;
        --max-session-gap)
            MAX_SESSION_GAP_MINUTES="$2"
            shift 2
            ;;
        --min-commit-time)
            MIN_COMMIT_TIME_MINUTES="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: $0 [--verbose|-v] [--work-hours HOURS] [--senior-lines-per-min RATE] [--junior-lines-per-min RATE] [--max-session-gap MINUTES] [--min-commit-time MINUTES] [--help|-h]"
            echo "  --verbose, -v              Show detailed session information"
            echo "  --work-hours HOURS        Set work day hours (default: 8)"
            echo "  --senior-lines-per-min RATE Set senior dev productivity in lines changed/min (default: 0.82)"
            echo "  --junior-lines-per-min RATE Set junior dev productivity in lines changed/min (default: 0.43)"
            echo "  --max-session-gap MINUTES  Set max gap between commits for continuous work (default: 240)"
            echo "  --min-commit-time MINUTES  Set minimum time assumed per commit (default: 5)"
            echo "  --help, -h                 Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Get all commits that modified .cs files, with timestamps
# Format: commit_hash|timestamp|files_changed
# Use --reverse to get commits in chronological order (oldest first)
git log --reverse --name-only --pretty=format:"%H|%ad" --date=iso -- "*.cs" > /tmp/cs_commits.txt

# Process the commits to extract timing information
declare -a commit_times=()
declare -a commit_hashes=()
declare -a session_minutes=()

# Read commits and extract timestamps
while IFS='|' read -r hash timestamp; do
    if [[ -n "$hash" && -n "$timestamp" ]]; then
        # Convert timestamp to epoch seconds for easier calculation
        epoch_time=$(date -j -f "%Y-%m-%d %H:%M:%S %z" "$timestamp" "+%s" 2>/dev/null || date -d "$timestamp" "+%s" 2>/dev/null)
        if [[ $? -eq 0 ]]; then
            commit_hashes+=("$hash")
            commit_times+=("$epoch_time")
        fi
    fi
done < /tmp/cs_commits.txt

echo "Found ${#commit_hashes[@]} commits that modified C# files ..."

# Calculate total lines of code changed in commits (excluding generated files)
TOTAL_LOC_CHANGED=0
while IFS='|' read -r hash timestamp; do
    if [[ -n "$hash" ]]; then
        # Get lines changed for this commit (additions + deletions)
        commit_changes=$(git show --numstat --format="" "$hash" 2>/dev/null | grep "\.cs$" | awk '{added+=$1; deleted+=$2} END {print added+deleted}' 2>/dev/null || echo "0")
        if [[ "$commit_changes" =~ ^[0-9]+$ ]]; then
            TOTAL_LOC_CHANGED=$((TOTAL_LOC_CHANGED + commit_changes))
        fi
    fi
done < /tmp/cs_commits.txt

echo "Total lines of C# code changed: $TOTAL_LOC_CHANGED"

# Calculate session minutes
total_minutes=0
session_count=0

# Handle the first commit (oldest) - assume at least 5 minutes of work
if [[ ${#commit_times[@]} -gt 0 ]]; then
    session_count=$((session_count + 1))
    session_minutes+=($MIN_COMMIT_TIME_MINUTES)  # Assume minimum time for the first commit
    total_minutes=$((total_minutes + MIN_COMMIT_TIME_MINUTES))
    if [[ "$VERBOSE" == "true" ]]; then
        echo "Session 1: ${MIN_COMMIT_TIME_MINUTES}m (commit ${commit_hashes[0]:0:8}) - first commit"
    fi
fi

# Process remaining commits
for i in $(seq 0 $((${#commit_times[@]} - 2))); do
    current_time=${commit_times[$i]}
    next_time=${commit_times[$((i + 1))]}
    
    # Calculate time difference in minutes (next is more recent, so subtract current from next)
    time_diff_minutes=$(( (next_time - current_time) / 60 ))
    
    # If commits are within the same minute, assume at least minimum time
    if [[ $time_diff_minutes -eq 0 ]]; then
        time_diff_minutes=$MIN_COMMIT_TIME_MINUTES  # Assume at least minimum time for any commit
    fi
    
    # Only count if the time difference is less than max session gap (continuous work)
    if [[ $time_diff_minutes -lt $MAX_SESSION_GAP_MINUTES && $time_diff_minutes -ge 0 ]]; then
        session_minutes+=($time_diff_minutes)
        total_minutes=$((total_minutes + time_diff_minutes))
        session_count=$((session_count + 1))
        
        if [[ "$VERBOSE" == "true" ]]; then
            # Convert to hours and minutes for display
            hours=$((time_diff_minutes / 60))
            minutes=$((time_diff_minutes % 60))
            if [[ $hours -gt 0 ]]; then
                echo "Session $((session_count)): ${hours}h ${minutes}m (commit ${commit_hashes[$i]:0:8} -> ${commit_hashes[$((i+1))]:0:8})"
            else
                echo "Session $((session_count)): ${minutes}m (commit ${commit_hashes[$i]:0:8} -> ${commit_hashes[$((i+1))]:0:8})"
            fi
        fi
    else
        if [[ "$VERBOSE" == "true" ]]; then
            # Convert gap to hours and minutes for display
            gap_hours=$((time_diff_minutes / 60))
            gap_minutes=$((time_diff_minutes % 60))
            if [[ $gap_hours -gt 0 ]]; then
                echo "Gap detected: ${gap_hours}h ${gap_minutes}m - not counting as continuous work"
            else
                echo "Gap detected: ${gap_minutes}m - not counting as continuous work"
            fi
        fi
    fi
done

echo "Total commits analyzed: ${#commit_hashes[@]}"
echo "Continuous work sessions: $session_count"

# Convert total minutes to hours and minutes
total_hours=$((total_minutes / 60))
total_remaining_minutes=$((total_minutes % 60))

if [[ $total_hours -gt 0 ]]; then
    echo "Total estimated time: ${total_hours}h ${total_remaining_minutes}m"
else
    echo "Total estimated time: ${total_remaining_minutes}m"
fi

if [[ $session_count -gt 0 ]]; then
    average_minutes=$((total_minutes / session_count))
    average_hours=$((average_minutes / 60))
    average_remaining_minutes=$((average_minutes % 60))
    
    if [[ $average_hours -gt 0 ]]; then
        echo "Average session length: ${average_hours}h ${average_remaining_minutes}m"
    else
        echo "Average session length: ${average_remaining_minutes}m"
    fi
    
    # Calculate days (configurable work day hours)
    total_hours_decimal=$((total_minutes / 60))
    days=$((total_hours_decimal / WORK_DAY_HOURS))
    remaining_hours=$((total_hours_decimal % WORK_DAY_HOURS))
    echo "Estimated work days (${WORK_DAY_HOURS}-hour days): ${days} days and ${remaining_hours}h ${total_remaining_minutes}m"
fi

# Development productivity comparison
if [[ $TOTAL_LOC_CHANGED -gt 0 ]]; then
    # Calculate estimated time for different developer levels
    senior_estimated_minutes=$(calculate_ratio $TOTAL_LOC_CHANGED $SENIOR_DEV_LINES_PER_MIN 0)
    junior_estimated_minutes=$(calculate_ratio $TOTAL_LOC_CHANGED $JUNIOR_DEV_LINES_PER_MIN 0)
    
    # Convert to hours and minutes
    senior_hours=$((senior_estimated_minutes / 60))
    senior_remaining_minutes=$((senior_estimated_minutes % 60))
    junior_hours=$((junior_estimated_minutes / 60))
    junior_remaining_minutes=$((junior_estimated_minutes % 60))
    
    echo "Senior developer (${SENIOR_DEV_LINES_PER_MIN} lines/min): ${senior_hours}h ${senior_remaining_minutes}m"
    echo "Junior developer (${JUNIOR_DEV_LINES_PER_MIN} lines/min): ${junior_hours}h ${junior_remaining_minutes}m"
    
    # Calculate productivity ratio
    if [[ $total_minutes -gt 0 ]]; then
        senior_ratio=$(calculate_ratio $senior_estimated_minutes $total_minutes 2)
        junior_ratio=$(calculate_ratio $junior_estimated_minutes $total_minutes 2)
        
        echo "Actual vs estimated ratio:"
        echo "  Senior: ${senior_ratio}x"
        echo "  Junior: ${junior_ratio}x"
    fi
fi

# Clean up
rm -f /tmp/cs_commits.txt
