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
    
    // Initialize with current date
    const today = new Date();
    const nextMonth = new Date(today);
    nextMonth.setMonth(today.getMonth() + 1);
    
    // Set minimum date for custom end date picker
    $("#customEndDate").attr("min", formatDate(nextMonth));
    
    // Set a default end date (1 year from now)
    const oneYearFromNow = new Date(today);
    oneYearFromNow.setFullYear(today.getFullYear() + 1);
    $("#customEndDate").val(formatDate(oneYearFromNow));
    
    // Update total payment when rental duration changes
    $("#rentalDuration").change(function() {
        const selectedOption = $(this).val();
        
        if (selectedOption === "custom") {
            // Show custom date picker
            $("#customDateContainer").show();
            
            // Set total based on the selected end date
            updateCustomDuration();
        } else {
            // Hide custom date picker
            $("#customDateContainer").hide();
            
            // Calculate based on selected months
            const duration = parseInt(selectedOption);
            const monthlyPrice = config.propertyPrice;
            const totalPrice = monthlyPrice * duration;
            
            // Update the UI
            $("#durationText").text(duration === 1 ? "1 month" : duration + " months");
            $("#totalPayment").text("₱" + totalPrice.toLocaleString());
            $("#totalMonths").val(duration);
        }
    });
    
    // Handle custom end date changes
    $("#customEndDate").change(function() {
        updateCustomDuration();
    });
    
    // Function to update duration and total payment for custom end date
    function updateCustomDuration() {
        const startDate = new Date();
        const endDate = new Date($("#customEndDate").val());
        
        // Calculate months difference
        const monthsDiff = calculateMonthsBetween(startDate, endDate);
        
        if (monthsDiff < 1) {
            alert("Minimum rental period is 1 month. Please select a later end date.");
            
            // Reset to 1 month in the future
            const oneMonthFromNow = new Date();
            oneMonthFromNow.setMonth(oneMonthFromNow.getMonth() + 1);
            $("#customEndDate").val(formatDate(oneMonthFromNow));
            
            // Recalculate
            updateCustomDuration();
            return;
        }
        
        // Calculate total price
        const monthlyPrice = config.propertyPrice;
        const totalPrice = monthlyPrice * monthsDiff;
        
        // Update the UI
        $("#durationText").text("until " + formatDisplayDate(endDate) + " (" + monthsDiff + " months)");
        $("#totalPayment").text("₱" + totalPrice.toLocaleString());
        $("#totalMonths").val(monthsDiff);
        
        // Set the hidden custom end date field
        $("#hiddenCustomEndDate").val($("#customEndDate").val());
    }
    
    // Helper function to calculate months between two dates
    function calculateMonthsBetween(startDate, endDate) {
        const yearDiff = endDate.getFullYear() - startDate.getFullYear();
        const monthDiff = endDate.getMonth() - startDate.getMonth();
        const dayDiff = endDate.getDate() >= startDate.getDate() ? 0 : -1;
        
        return yearDiff * 12 + monthDiff + dayDiff;
    }
    
    // Helper function to format date as YYYY-MM-DD for input value
    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    
    // Helper function to format date for display
    function formatDisplayDate(date) {
        const options = { year: 'numeric', month: 'long', day: 'numeric' };
        return date.toLocaleDateString(undefined, options);
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