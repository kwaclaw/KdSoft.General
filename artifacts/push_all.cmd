For /R %%G IN (*.nupkg) do nuget push "%%G" -Source "http://192.168.0.32/QlineFeed/"