if(-not ('SnapshotFiles' -as [type])){Add-Type -Path (Join-Path $PSScriptRoot 'SnapshotFiles.cs')}
$script:lastSnapshotError=[DateTime]::MinValue
function Publish-Snapshot([string]$Path,[string[]]$Lines){
 $published=[SnapshotFiles]::Publish($Path,$Lines)
 if(-not $published -and ([DateTime]::UtcNow-$script:lastSnapshotError).TotalSeconds -ge 30){
  $script:lastSnapshotError=[DateTime]::UtcNow
  try{[IO.File]::AppendAllText((Join-Path $PSScriptRoot 'snapshot-errors.log'),[DateTime]::UtcNow.ToString('s')+' '+[IO.Path]::GetFileName($Path)+': '+[SnapshotFiles]::LastWriteError+[Environment]::NewLine)}catch{}
 }
 return $published
}
