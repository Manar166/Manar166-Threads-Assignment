```text
 R1 Run the project and compare Expected and Actual
 Expected: 400000
Actual:   284354


 why the numbers disagree?

 many threads Enter the code at the same time read non atomic process 
 
 Views++ is Read + modifiy + write

 whem many threads read the value at the same time they will read the same value and add the same value resulting in lost updates.


say when you would protect this kind of update differently?

Use lock when multiple operations or a condition before increment

```