#!/bin/sh

echo "Renaming Index.htm -> Index.html"
mv /app/coverageresults/reports/index.htm /app/coverageresults/reports/index.html

echo "Copy Codecoverage Results"
cp -R /app/coverageresults/. /var/coverageresults/

echo "Remove Codecoverage Results"
rm -R /app/coverageresults/