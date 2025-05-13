/**
 * Property Details Page JavaScript
 * Handles interactive features for property details
 */

function initPropertyDetails(config) {
    console.log("Property Details Config:", config);
    console.log("PropertyID Value:", $("#inquiryPropertyId").val());

    // Check CSRF token availability
    const tokenElement = $('input[name="__RequestVerificationToken"]');
    console.log("CSRF Token Present:", tokenElement.length > 0);

    // Add hover effect to property details items
    $('.hover-light').hover(
        function () { $(this).addClass('bg-light'); },
        function () { $(this).removeClass('bg-light'); }
    );

    if (!config.isUserLoggedIn) {
        console.log("User not logged in - inquiry functionality disabled");
        return; // Exit if user is not logged in
    }

    // Test API connection
    $.get('/api/Inquiry/test')
        .done(function (data) {
            console.log("API Test Success:", data);
        })
        .fail(function (xhr) {
            console.error("API Test Failed:", xhr.responseText);
        });

    // Handle inquiry submission
    $("#submitInquiry").click(function () {
        if (!$("#inquiryForm")[0].checkValidity()) {
            $("#inquiryForm")[0].reportValidity();
            return;
        }

        const propertyId = $("#inquiryPropertyId").val();
        const subject = $("#inquirySubject").val();
        const message = $("#inquiryMessage").val();

        console.log("Submitting inquiry:", {
            PropertyId: propertyId,
            Subject: subject,
            Message: message
        });

        // Send AJAX request to submit inquiry
        $.ajax({
            url: '/api/Inquiry',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                PropertyId: propertyId,
                Subject: subject,
                Message: message
            }),
            success: function (response) {
                var responseText = config.hasAgent ? "The agent" : "Our team";
                Swal.fire({
                    title: 'Success!',
                    text: 'Inquiry sent! ' + responseText + ' will respond to you soon.',
                    icon: 'success',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#4e73df'
                });
                $("#inquiryModal").modal("hide");

                // Clear form
                $("#inquirySubject").val('');
                $("#inquiryMessage").val('');
            },
            error: function (xhr, status, error) {
                console.error("Error sending inquiry:", xhr.responseText);

                let errorMessage = 'An error occurred while sending your inquiry.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }

                Swal.fire({
                    title: 'Error!',
                    text: errorMessage,
                    icon: 'error',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#e74a3b'
                });
            }
        });
    });

    // Initialize other property details functionalities
    initImageGallery();
}

function initImageGallery() {
    // Handle image gallery navigation
    $('.thumbnail-image').click(function () {
        const imgSrc = $(this).attr('src');
        $('#mainPropertyImage').attr('src', imgSrc);
    });
} 