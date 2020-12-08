# Application Architecture

This file will describe various design aspects and capabilities that are part of the NGSA Platform Validation Application.  This application is meant to act as a production level service that is intended to deployed to a Kubernetes cluster and used for various forms of validation testing.

## High Level Architecture

TODO

## Data Access Design

We wish to fully separate the service used to read and write data from the front-end services that act as brokers of the data to the calling clients.  This provides deployment flexibility and isolates interactions with the data which eases synchronization needs, data caching implementation, and access semantics for the clients.

```
OLD NGSA POD
+-----------------------+              XXXXXXXXX
|                       |           XXXX       XXXX
|    Main               |  https   XX             XX
|    +---------+        |  :443    XX             XX
|    |         +-----------------> XXXXX       XXXXX
|    | NGSA    |        |          X   XXXXXXXXX   X
|    | Web     +<-----------------+X               X
|    | Service |        |          X   CosmosDB    X
|    |         |        |          X               X
|    |         |        |          X               X
|    +---------+        |          XX             XX
|                       |           XXX         XXX
|                       |             XXXXXXXXXXX
+-----------------------+             


NGSA POD
+---------------------------------+
|                                 |              XXXXXXXXX
|                                 |           XXXX       XXXX
| Main                Sidecar     |  https   XX             XX
| +---------+  https  +---------+ |  :443    XX             XX
| |         |  :4122  |         +----------> XXXXX       XXXXX
| | NGSA    +-------->+ NGSA    | |          X   XXXXXXXXX   X
| | Web     |         | Data    +<----------+X               X
| | Service +<--------+ Service | |          X   CosmosDB    X
| |         |         |         | |          X               X
| |         |         |         | |          X               X
| +---------+         +---------+ |          XX             XX
|                                 |           XXX         XXX
+---------------------------------+             XXXXXXXXXXX
                                                   
```
