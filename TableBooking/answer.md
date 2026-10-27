

```text

the final numbers of each run
Booked=6, TablesLeft=-1
Booked=9, TablesLeft=-4
Booked=8, TablesLeft=-3
Booked=7, TablesLeft=-2


The Problem :is many threads enters the code at the same time causing Race condition 
and lost update



Fix 
use Lock to allow just one thresd to Enter the code and modifiy

```