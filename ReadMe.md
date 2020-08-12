## Welcome to EVA Refueling Repo!

This is a KSP mod to let you refuel kerbals performing EVA without entering crew hatches. This can be useful if you need to refill EVA tanks and don't want to spend time on getting to a hatch or wasting internal atmosphere.

## How to use it

Some parts (EVAs and pods) are given EVARefuelingPump MODULEs. These MODULES can be either "EVASide" or not. You can connect only EVA+nonEVA pairs. Pumping rates are determined in PUMPING_RATES NODE of the nonEVA pump. Note that PUMPING_RATES uses internal resource names. In order to engage a pair, you need to 
1) use "Find Pumping Counterpart" buttons on both parts you want to connect. 
2) If the pair has been succesfully engaged, both parts will have "Cut Connection" buttons.
3) For each resource that is both present in tanks of a pair and in PUMPING_RATES of the nonEVA pump you will get "Pump Here buttons". They will initiate pumping towards the corresponding part.
4) If pumping is in process, you will have "Stop Pumping" buttons for each currently pumped resource.
5) Pumps can't be in two different pairs at the same time. Cut previous connection in order to change the pumping counterpart.
6) Pumping counterparts can be both on the same and different vessels.

## Other mods

EVARefueling needs ModuleManager to be any useful (inserting modules into parts - kerbal EVAs and pods).
Provided configs only let you pump MonoPropellant, Oxygen (CRP-ready) and Tape (useless as of now, planned for "Pay to Play" reliability mod field repairs).
