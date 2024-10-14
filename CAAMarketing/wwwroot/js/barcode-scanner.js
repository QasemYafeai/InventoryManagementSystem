let scanner;

function submitFormAfterScanning() {
    document.getElementById("filterForm").submit();
}

function scanBarcode() {
    const { BrowserBarcodeReader } = ZXing;
    const barcodeReader = new BrowserBarcodeReader();

    scanner = barcodeReader.decodeFromVideoDevice(undefined, "barcode-scanner", (result, err) => {
        if (result) {
            console.log("Barcode detected and processed: ", result.text);
            document.getElementById("scannedBarcode").value = result.text;
            document.getElementById("SearchString1").value = result.text;
            scanner.stop();
            $("#scanner-modal").modal("hide");

            // Call the function to submit the form after scanning
            submitFormAfterScanning();
        }
        if (err && !(err instanceof ZXing.NotFoundException)) {
            console.error(err);
        }
    });

    $("#scanner-modal").modal("show");
}



$("#scanner-modal").on("hidden.bs.modal", function () {
    if (scanner) {
        scanner.stop();
    }
});
