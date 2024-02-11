#!/bin/bash

# Function to report test results
function report_test_results {
    testName=$1
    isPassed=$2

    echo "##teamcity[testStarted name='$testName']"
    if [ "$isPassed" = true ]; then
        result="##teamcity[testPassed name='$testName']"
    else
        result="##teamcity[testFailed name='$testName' message='Test $testName failed.']"
    fi

    echo "$result"
    echo "##teamcity[testFinished name='$testName']"
}

mode=1
# Main script logic
echo "##teamcity[testSuiteStarted name='suiteName']"
if [ "$mode" -eq "1" ]; then
    # Report two green tests
    report_test_results "Test1" true
    report_test_results "Test2" true
elif [ "$mode" -eq "2" ]; then
    # Report first test failed and second green
    report_test_results "Test1" false
    report_test_results "Test2" true
elif [ "$mode" -eq "3" ]; then
    # Report first test failed and second green
    report_test_results "Test1" true
    report_test_results "Test2" false
elif [ "$mode" -eq "4" ]; then
    # Report first test failed and second green
    report_test_results "Test1" false
    report_test_results "Test2" false
else
    echo "##teamcity[testSuiteFinished name='suiteName']"
    echo "Build failure."
    exit 1
fi
echo "##teamcity[testSuiteFinished name='suiteName']"
