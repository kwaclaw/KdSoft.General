For /R %%G IN (*.nupkg) do nuget push "%%G" -Source "http://teamcity.qlinesolutions.com/QlineFeed/" -ApiKey "DB420C35-E51D-4B1C-A5F5-FF73732683B9"

pause