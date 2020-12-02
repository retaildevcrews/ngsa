export const queryErrorMessages = {
    invalidMovieIDMessage: "The parameter 'movieId' should start with 'tt' and be between 7 and 11 characters in total.",
    invalidActorIDMessage: "The parameter 'actorId' should start with 'nm' and be between 7 and 11 characters in total.",
    invalidQSearchMessage: "The parameter 'q' should be between 2 and 20 characters.",
    invalidPageNumberMessage: "The parameter 'pageNumber' should be between 1 and 10000.",
    invalidPageSizeMessage: "The parameter 'pageSize' should be between 1 and 1000.",
    invalidGenreMessage: "The parameter 'genre' should be between 3 and 20 characters.",
    invalidYearMessage: "The parameter 'year' should be between 1874 and 2025.",
    invalidRatingMessage: "The parameter 'rating' should be between 0.0 and 10.0."
};

// utilities for validating API parameters.
export class ParameterValidationUtilities {

    // validate common parameters

    public static validateQ(query: any) {
        
        if ("q" in query) {
            
            if (query.q === null || query.q === undefined || query.q.length < 2 || query.q.length > 20 ) {
                
                const message = { 
                    message: queryErrorMessages.invalidQSearchMessage,
                    code: "InvalidValue",
                    target: "q"
                };

                return { validated: false, message: message };
            }

        }

        return { validated: true };

    }

    public static validatePageNumber(query: any) {
        
        if ("pageNumber" in query) {
            
            const pageNumber = parseInt(query.pageNumber, 10)
            
            if (isNaN(pageNumber) || pageNumber != query.pageNumber || pageNumber < 1 || pageNumber > 10000) {
                
                const message = { 
                    message: queryErrorMessages.invalidPageNumberMessage,
                    code: "InvalidValue",
                    target: "pageNumber"
                };

                return { validated: false, message: message };
            }

        }

        return { validated: true };

    }

    public static validatePageSize(query: any) {
        
        if ("pageSize" in query) {

            const pageSize = parseInt(query.pageSize, 10)

            if (isNaN(pageSize) || pageSize != query.pageSize || pageSize < 1 || pageSize > 1000) {
                
                const message = { 
                    message: queryErrorMessages.invalidPageSizeMessage,
                    code: "InvalidValue",
                    target: "pageSize"
                };

                return { validated: false, message: message };
            }

        }

        return { validated: true };

    }

    public static validateGenre(query: any) {
        
        if ("genre" in query) {

            if (query.genre === null || query.genre === undefined || query.genre.length < 3 || query.genre.length > 20) {
                
                const message = { 
                    message: queryErrorMessages.invalidGenreMessage,
                    code: "InvalidValue",
                    target: "genre"
                };

                return { validated: false, message: message };

            }

        }

        return { validated: true };

    }

    public static validateYear(query: any) {
        
        if ("year" in query) {
            const year = parseInt(query.year, 10);

            if (isNaN(year) || year != query.year || year < 1874 || year > 2025) {
                
                const message = { 
                    message: queryErrorMessages.invalidYearMessage,
                    code: "InvalidValue",
                    target: "year"
                };

                return { validated: false, message: message };

            }

        }

        return { validated: true };

    }

    public static validateRating(query: any) {
        
        if ("rating" in query) {

            const rating = parseFloat(query.rating);

            if (isNaN(rating) || rating != query.rating || rating < 0 || rating > 10) {

                const message = { 
                    message: queryErrorMessages.invalidRatingMessage,
                    code: "InvalidValue",
                    target: "rating"
                };

                return { validated: false, message: message };
            }

        }

        return { validated: true };

    }

    public static validateMovieId(movieId: string) {
        
        let validated = true;

        const message = { 
            message: queryErrorMessages.invalidMovieIDMessage,
            code: "InvalidValue",
            target: "movieId"
        };

        if ( movieId === null ||
            movieId === undefined ||
            movieId.length < 7 ||
            movieId.length > 11 ||
            movieId.substring(0,2) !== "tt" ) {
                validated = false;
                return { validated: validated, message: message };
        } else {
            const val = parseInt(movieId.substring(2), 10);
            if (isNaN(val) || val <= 0) {
                validated = false;
                return { validated: validated, message: message };
            }
        }

        return { validated: validated };
    }

    public static validateActorId(actorId: string) {
        
        let validated = true;

        const message = { 
            message: queryErrorMessages.invalidActorIDMessage,
            code: "InvalidValue",
            target: "actorId"
        };                

        if ( actorId === null ||
            actorId === undefined ||
            actorId.length < 7 ||
            actorId.length > 11 ||
            actorId.substring(0,2) !== "nm" ) {
                validated = false;
                return { validated: validated, message: message };
        } else {
            const val = parseInt(actorId.substring(2), 10);
            if (isNaN(val) || val <= 0) {
                validated = false;
                return { validated: validated, message: message };
            }
        }
        
        return { validated: validated };
    }
}
