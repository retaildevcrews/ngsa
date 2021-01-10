// utilities for date/time functions
export class DateUtilities {

    public static getTimer() {
        const start: number = Date.now();
        return () => Date.now() - start;
    }

    // calculate duration (in ms) from node process hrtime
    public static getDurationMS(hrtime: [number, number]): string {

        // convert to milliseconds
        const duration = ((hrtime[0] * 1e9) + hrtime[1]) / 1e6;

        return duration.toFixed(0);
    }
}
