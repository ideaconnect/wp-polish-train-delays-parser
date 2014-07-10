wp-polish-train-delays-parser
=============================

Library which parser Polish rail operators' timetables to provide delays information

Consists two projects:

- polish-train-delay-parser: the main library
- TrainDelaysExample: Windows Phone sample application which uses the library

Quite well documented within the code.

Class was made for own usage - never really meant to reach public therefore it consists a lot of coding issues, yet... it works :) [2014-07-11]

Usage
=====

Add a reference to the library and use the namespace **IDCT**.

Assign new object using:

    TrainDelays delaysInstance = new TrainDelays();
    
Assign the delegates:

    delaysInstance.errorOccured += delaysInstance_errorOccured;
    delaysInstance.wynikiSzukania += delaysInstance_wynikiSzukania;
    delaysInstance.wynikiStacja += delaysInstance_wynikiStacja;
    delaysInstance.wynikiPociag += delaysInstance_wynikiPociag;

Where:

* wynikiSzukania - delegate which handles station search
* wynikiStacja - delegate which handles output of trains on the station
* wynikiPociag - delegate which handles stations (stops) for the particular train

For one of the rail operators the library can also fetch GPS data of the train.