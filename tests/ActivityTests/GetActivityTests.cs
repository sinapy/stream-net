using NUnit.Framework;
using Stream.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNetTests
{
    [TestFixture]
    public class GetActivityTests : TestBase
    {
        [Test]
        public async Task TestGet()
        {
            var newActivity = new Activity("1", "test", "1");
            var first = await this.UserFeed.AddActivityAsync(newActivity);

            newActivity = new Activity("1", "test", "2");
            var second = await this.UserFeed.AddActivityAsync(newActivity);

            newActivity = new Activity("1", "test", "3");
            var third = await this.UserFeed.AddActivityAsync(newActivity);

            var activities = (await this.UserFeed.GetActivitiesAsync(0, 2)).Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());
            Assert.AreEqual(third.Id, activities.First().Id);
            Assert.AreEqual(second.Id, activities.Skip(1).First().Id);

            activities = (await this.UserFeed.GetActivitiesAsync(1, 2)).Results;
            Assert.AreEqual(second.Id, activities.First().Id);

            // $id_offset =  ['id_lt' => $third_id];
            activities = (await this.UserFeed.GetActivitiesAsync(0, 2, FeedFilter.Where().IdLessThan(third.Id))).Results;
            Assert.AreEqual(second.Id, activities.First().Id);
        }

        [Test]
        public async Task TestGetFlatActivities()
        {
            var newActivity = new Activity("1", "test", "1");
            var first = await this.UserFeed.AddActivityAsync(newActivity);

            newActivity = new Activity("1", "test", "2");
            var second = await this.UserFeed.AddActivityAsync(newActivity);

            newActivity = new Activity("1", "test", "3");
            var third = await this.UserFeed.AddActivityAsync(newActivity);

            var response = await this.UserFeed.GetFlatActivitiesAsync(GetOptions.Default.WithLimit(2));
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Duration);
            var activities = response.Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());
            Assert.AreEqual(third.Id, activities.First().Id);
            Assert.AreEqual(second.Id, activities.Skip(1).First().Id);

            response = await this.UserFeed.GetFlatActivitiesAsync(GetOptions.Default.WithOffset(1).WithLimit(2));
            activities = response.Results;
            Assert.AreEqual(second.Id, activities.First().Id);

            response = await this.UserFeed.GetFlatActivitiesAsync(GetOptions.Default.WithLimit(2).WithFilter(FeedFilter.Where().IdLessThan(third.Id)));
            activities = response.Results;
            Assert.AreEqual(second.Id, activities.First().Id);

            response = await this.UserFeed.GetFlatActivitiesAsync(GetOptions.Default.WithLimit(2).WithSession("dummy").WithFilter(FeedFilter.Where().IdLessThan(third.Id)));
            activities = response.Results;
            Assert.AreEqual(second.Id, activities.First().Id);
        }

        [Test]
        public async Task TestGetActivitiesByID()
        {
            var newActivity1 = new Activity("1", "test", "1");
            var newActivity2 = new Activity("1", "test", "2");
            var newActivity3 = new Activity("1", "other", "2");
            var addedActivities = new List<Activity>();

            var response = await this.UserFeed.AddActivityAsync(newActivity1);
            addedActivities.Add(response);
            response = await this.UserFeed2.AddActivityAsync(newActivity2);
            addedActivities.Add(response);
            response = await this.FlatFeed.AddActivityAsync(newActivity3);
            addedActivities.Add(response);

            var activities = (await Client.Batch.GetActivitiesByIdAsync(addedActivities.Select(a => a.Id))).Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(addedActivities.Count, activities.Count());

            activities.ForEach(a =>
            {
                var found = addedActivities.Find(x => x.Id == a.Id);
                Assert.NotNull(found);
                Assert.AreEqual(found.Actor, a.Actor);
                Assert.AreEqual(found.Object, a.Object);
                Assert.AreEqual(found.Verb, a.Verb);
            });
        }

        [Test]
        public async Task TestGetActivitiesByForeignIDAndTime()
        {
            var newActivity1 = new Activity("1", "test", "1")
            {
                ForeignId = "fid-test-1",
                Time = DateTime.Parse("2000-08-16T16:32:32"),
            };

            var newActivity2 = new Activity("1", "test", "2")
            {
                ForeignId = "fid-test-2",
                Time = DateTime.Parse("2000-08-17T16:32:32"),
            };

            var newActivity3 = new Activity("1", "other", "2")
            {
                ForeignId = "fid-other-1",
                Time = DateTime.Parse("2000-08-19T16:32:32"),
            };

            var addedActivities = new List<Activity>();

            var response = await this.UserFeed.AddActivityAsync(newActivity1);
            addedActivities.Add(response);
            response = await this.UserFeed2.AddActivityAsync(newActivity2);
            addedActivities.Add(response);
            response = await this.FlatFeed.AddActivityAsync(newActivity3);
            addedActivities.Add(response);

            var activities = (await Client.Batch.GetActivitiesByForeignIdAsync(
                addedActivities.Select(a => new ForeignIdTime(a.ForeignId, a.Time.Value)))).Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(addedActivities.Count, activities.Count());

            activities.ForEach(a =>
            {
                var found = addedActivities.Find(x => x.Id == a.Id);
                Assert.NotNull(found);
                Assert.AreEqual(found.Actor, a.Actor);
                Assert.AreEqual(found.Object, a.Object);
                Assert.AreEqual(found.Verb, a.Verb);
                Assert.AreEqual(found.ForeignId, a.ForeignId);
                Assert.AreEqual(found.Time, a.Time);
            });
        }
    }
}