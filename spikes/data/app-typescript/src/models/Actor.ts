import { Movie } from "./Movie";

export class Actor {

    public id: string;
    public actorId: string;
    public name: string;
    public textSearch: string;
    public type: string;
    public readonly partitionKey: string;
    public birthYear?: number;
    public deathYear?: number;
    public profession?: string[];
    public movies?: Movie[];

    constructor(data?: any) {
        if (data) {
            this.id = data.id;
            this.actorId = data.actorId;
            this.name = data.name;
            this.textSearch = data.textSearch;
            this.type = data.type;
            this.partitionKey = data.partitionKey;
            this.birthYear = data.birthYear;
            this.deathYear = data.deathYear;
            this.profession = data.profession;
            this.movies = data.movies;
        }
    }

    // compute the partition key based on the actorId
    public static computePartitionKey(id: string): string {
        let idInt = 0;

        if ( id.length > 5 && id.startsWith("nm")) {
            idInt = parseInt(id.substring(2), 10);
            return isNaN(idInt) ? "0" : (idInt % 10).toString();
        }

        return idInt.toString();
    }
}
