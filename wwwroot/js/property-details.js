/**
 * Property Details Page JavaScript
 * Handles interactive features for property details
 */

function initPropertyDetails(config) {
    // Add hover effect to property details items
    $('.hover-light').hover(
        function() { $(this).addClass('bg-light'); },
        function() { $(this).removeClass('bg-light'); }
    );
    
    if (!config.isUserLoggedIn) {
        return; // Exit if user is not logged in
    }
    
    // Handle inquiry submission
    $("#submitInquiry").click(function() {
        if (!$("#inquiryForm")[0].checkValidity()) {
            $("#inquiryForm")[0].reportValidity();
            return;
        }
        
        // Here you would add the AJAX call to submit the inquiry
        var responseText = config.hasAgent ? "The agent" : "Our team";
        alert("Inquiry sent! " + responseText + " will respond to you soon.");
        $("#inquiryModal").modal("hide");
    });
    
    // Handle rent/buy action submission
    $("#submitAction").click(function() {
        if (!$("#actionForm")[0].checkValidity()) {
            $("#actionForm")[0].reportValidity();
            return;
        }
        
        // Get selected payment method
        let paymentMethod = $("input[name='paymentMethod']:checked").attr('id');
        let paymentText = "";
        
        if (paymentMethod === "paymentBank") {
            paymentText = "Bank Transfer";
        } else if (paymentMethod === "paymentCard") {
            paymentText = "Credit/Debit Card";
        } else if (paymentMethod === "paymentMortgage") {
            paymentText = "Mortgage/Home Loan";
        }
        
        // Use the variables we defined at the top
        var requestType = config.isRental ? "rental" : "purchase";
        let confirmationMessage = "";
        
        if (!config.hasAgent) {
            confirmationMessage = "Your " + requestType + " request has been submitted with " + paymentText + " as your payment method.\nThe EstateEase team will process your payment and contact you shortly.";
        } else {
            confirmationMessage = "Your " + requestType + " request has been submitted with " + paymentText + " as your payment method.\nOur team will contact you shortly to proceed with the transaction.";
        }
        
        // Here you would add the AJAX call to submit the action
        alert(confirmationMessage);
        $("#actionModal").modal("hide");
    });
} 