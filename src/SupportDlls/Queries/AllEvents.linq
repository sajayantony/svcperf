<Query Kind="Statements">
  <Connection>
    <ID>bceb0ed6-52bc-45f7-b629-a9ea8ae98bbb</ID>
    <Driver Assembly="TxLinqPadDriver" PublicKeyToken="3d3a4b0768c9178e">TxLinqPadDriver.TxDataContextDriver</Driver>
    <DriverData>
      <ContextName>http</ContextName>
      <Files>c:\TxSamples\LINQPad\Traces\HTTP_Server.etl;</Files>
      <MetadataFiles>c:\TxSamples\LINQPad\Manifests\HTTP_Server.man;</MetadataFiles>
      <IsRealTime>false</IsRealTime>
      <IsUsingDirectoryLookup>false</IsUsingDirectoryLookup>
    </DriverData>
  </Connection>
  <Namespace>Microsoft.Etw.Microsoft_Windows_HttpService</Namespace>
</Query>
var all = playback.GetObservable<SystemEvent>();
var allevents  = from e in playback.BufferOutput(all)
                  select new  
                  { 
                      Name = e.GetType().Name,
                      Provider = e.Header.ProviderId, 
                      Version = e.Header.Version,
                      Id = e.Header.EventId, 
                      Opcode = e.Header.Opcode, 
                      Task = e.Header.Task, 
                      Pid = e.Header.ProcessId,
                      Tid = e.Header.ThreadId, 
                      Activity = e.Header.ActivityId,
                      RelatedActividy = e.Header.RelatedActivityId,
                      Message = e.ToString(),				 
                  };
playback.Run();
allevents.Dump();

